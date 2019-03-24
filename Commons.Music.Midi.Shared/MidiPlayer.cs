using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public enum PlayerState
	{
		Stopped,
		Playing,
		Paused,
	}

	// Player implementation. Plays a MIDI song synchronously.
	class MidiSyncPlayer : IDisposable
	{
		public MidiSyncPlayer (MidiMusic music)
			: this (music, new SimpleAdjustingMidiPlayerTimeManager ())
		{
		}
		
		public MidiSyncPlayer (MidiMusic music, IMidiPlayerTimeManager timeManager)
		{
			if (music == null)
				throw new ArgumentNullException ("music");
			time_manager = timeManager;

			this.music = music;
			messages = SmfTrackMerger.Merge (music).Tracks [0].Messages;
			state = PlayerState.Stopped;
		}

		public event Action Finished;
		public event Action PlaybackCompletedToEnd;

		internal MidiMusic music;
		internal IList<MidiMessage> messages;
		ManualResetEvent pause_handle = new ManualResetEvent (false);
		internal PlayerState state;
		bool do_pause, do_stop;
		IMidiPlayerTimeManager time_manager;
		
		public int PlayDeltaTime { get; set; }
		
		public TimeSpan PositionInTime {
			// FIXME: this is not exact after seek operation.
			get { return GetTimerOffsetWithTempoRatio () + playtime_delta; }
			// get { return TimeSpan.FromMilliseconds (music.GetTimePositionInMillisecondsForTick (PlayDeltaTime)); }
		}

		public double TempoChangeRatio {
			get { return tempo_ratio; }
			set {
				playtime_delta += GetTimerOffsetWithTempoRatio ();
				timer_resumed = DateTime.Now;
				tempo_ratio = value;
			}
		}
		
		TimeSpan GetTimerOffsetWithTempoRatio ()
		{
			switch (state) {
			case PlayerState.Playing:
				return TimeSpan.FromMilliseconds ((DateTime.Now - timer_resumed).TotalMilliseconds * tempo_ratio);
			}
			return TimeSpan.Zero;
		}

		public virtual void Dispose ()
		{
			if (state != PlayerState.Stopped)
				Stop ();
			Mute ();
		}

		public void Play ()
		{
			pause_handle.Set ();
			timer_resumed = DateTime.Now;
			state = PlayerState.Playing;
		}

		void AllControlReset ()
		{
			for (int i = 0; i < 16; i++)
				OnEvent (new MidiEvent ((byte) (i + 0xB0), 0x79, 0, null));
		}

		void Mute ()
		{
			for (int i = 0; i < 16; i++)
				OnEvent (new MidiEvent ((byte) (i + 0xB0), 0x78, 0, null));
		}

		public void Pause ()
		{
			do_pause = true;
			playtime_delta += DateTime.Now - timer_resumed;
			timer_resumed = DateTime.Now;
			Mute ();
		}

		int event_idx = 0;

		public void PlayerLoop ()
		{
			AllControlReset ();
			playtime_delta = TimeSpan.Zero;
			event_idx = 0;
			PlayDeltaTime = 0;
			while (true) {
				pause_handle.WaitOne ();
				if (do_stop)
					break;
				if (do_pause) {
					pause_handle.Reset ();
					do_pause = false;
					state = PlayerState.Paused;
					continue;
				}
				if (event_idx == messages.Count)
					break;
				HandleEvent (messages [event_idx++]);
			}
			do_stop = false;
			Mute ();
			state = PlayerState.Stopped;
			if (event_idx == messages.Count)
				if (PlaybackCompletedToEnd != null)
					PlaybackCompletedToEnd ();
			if (Finished != null)
				Finished ();
		}

		internal int current_tempo = MidiMetaType.DefaultTempo;
		internal byte [] current_time_signature = new byte [4];
		double tempo_ratio = 1.0;
		DateTime timer_resumed;
		TimeSpan playtime_delta;

		int GetDeltaTimeInMilliseconds (int deltaTime)
		{
			if (music.DeltaTimeSpec < 0)
				throw new NotSupportedException ("SMPTe-basd delta time is not implemented yet");
			return (int) (current_tempo / 1000 * deltaTime / music.DeltaTimeSpec / tempo_ratio);
		}

		public virtual void HandleEvent (MidiMessage m)
		{
			if (seek_processor != null) {
				var result = seek_processor.FilterMessage (m);
				switch (result) {
				case SeekFilterResult.PassAndTerminate:
				case SeekFilterResult.BlockAndTerminate:
					seek_processor = null;
					break;
				}

				switch (result) {
				case SeekFilterResult.Block:
				case SeekFilterResult.BlockAndTerminate:
					return; // ignore this event
				}
			}
			else if (m.DeltaTime != 0) {
				var ms = GetDeltaTimeInMilliseconds (m.DeltaTime);
				time_manager.WaitBy (ms);
			}
			
			if (m.Event.StatusByte == 0xFF) {
				if (m.Event.Msb == MidiMetaType.Tempo)
					current_tempo = MidiMetaType.GetTempo (m.Event.Data);
				else if (m.Event.Msb == MidiMetaType.TimeSignature && m.Event.Data.Length == 4)
					Array.Copy (m.Event.Data, current_time_signature, 4);
			}

			OnEvent (m.Event);
			PlayDeltaTime += m.DeltaTime;
		}

		public MidiEventAction EventReceived;

		protected virtual void OnEvent (MidiEvent m)
		{
			if (EventReceived != null)
				EventReceived (m);
		}

		public void Stop ()
		{
			if (state != PlayerState.Stopped) {
				do_stop = true;
				if (pause_handle != null)
					pause_handle.Set ();
				if (Finished != null)
					Finished ();
			}
		}

		private ISeekProcessor seek_processor;

		// not sure about the interface, so make it non-public yet.
		internal void Seek (ISeekProcessor seekProcessor, int ticks)
		{
			seek_processor = seekProcessor ?? new SimpleSeekProcessor (ticks);
			event_idx = 0;
			PlayDeltaTime = ticks;
			timer_resumed = DateTime.Now;
			playtime_delta = TimeSpan.FromMilliseconds (music.GetTimePositionInMillisecondsForTick (ticks));
			Mute ();
		}
	}

	// Provides asynchronous player control.
	public class MidiPlayer : IDisposable
	{
		MidiSyncPlayer player;
		Task sync_player_task;

		public MidiPlayer (MidiMusic music)
			: this (music, MidiAccessManager.Empty)
		{
		}

		public MidiPlayer (MidiMusic music, IMidiAccess access)
			: this (music, access, new SimpleAdjustingMidiPlayerTimeManager ())
		{
		}

		public MidiPlayer (MidiMusic music, IMidiOutput output)
			: this (music, output, new SimpleAdjustingMidiPlayerTimeManager ())
		{
		}
		
		public MidiPlayer (MidiMusic music, IMidiPlayerTimeManager timeManager)
			: this (music, MidiAccessManager.Empty, timeManager)
		{
		}
		
		public MidiPlayer (MidiMusic music, IMidiAccess access, IMidiPlayerTimeManager timeManager)
			: this (music, access.OpenOutputAsync (access.Outputs.First ().Id).Result, timeManager)
		{
			should_dispose_output = true;
		}
		
		public MidiPlayer (MidiMusic music, IMidiOutput output, IMidiPlayerTimeManager timeManager)
		{
			if (music == null)
				throw new ArgumentNullException ("music");
			if (output == null)
				throw new ArgumentNullException ("output");
			if (timeManager == null)
				throw new ArgumentNullException ("timeManager");
			
			this.output = output;

			player = new MidiSyncPlayer (music, timeManager);
			EventReceived += (m) => {
				switch (m.EventType) {
				case MidiEvent.NoteOn:
				case MidiEvent.NoteOff:
					if (channel_mask != null && channel_mask [m.Channel])
						return; // ignore messages for the masked channel.
					goto default;
				case MidiEvent.SysEx1:
				case MidiEvent.SysEx2:
					if (buffer.Length <= m.Data.Length)
						buffer = new byte [buffer.Length * 2];
					buffer [0] = m.StatusByte;
					Array.Copy (m.Data, 0, buffer, 1, m.Data.Length);
					output.Send (buffer, 0, m.Data.Length + 1, 0);
					break;
				case MidiEvent.Meta:
					// do nothing.
					break;
				default:
					var size = MidiEvent.FixedDataSize (m.StatusByte);
					buffer [0] = m.StatusByte;
					buffer [1] = m.Msb;
					buffer [2] = m.Lsb;
					output.Send (buffer, 0, size + 1, 0);
					break;
				}
			};
		}

		IMidiOutput output;
		bool should_dispose_output;
		byte [] buffer = new byte [0x100];
		bool [] channel_mask;

		public event Action Finished {
			add { player.Finished += value; }
			remove { player.Finished -= value; }
		}

		public event Action PlaybackCompletedToEnd {
			add { player.PlaybackCompletedToEnd += value; }
			remove { player.PlaybackCompletedToEnd -= value; }
		}

		public PlayerState State {
			get { return player.state; }
		}

		public double TempoChangeRatio {
			get { return player.TempoChangeRatio; }
			set { player.TempoChangeRatio = value; }
		}

		public int Tempo {
			get { return player.current_tempo; }
		}
		
		public int Bpm {
			get { return (int) (60.0 / Tempo * 1000000.0); }
		}
		
		// You can break the data at your own risk but I take performance precedence.
		public byte [] TimeSignature {
			get { return player.current_time_signature; }
		}

		public int PlayDeltaTime {
			get { return player.PlayDeltaTime; }
		}
		
		public TimeSpan PositionInTime {
			get { return player.PositionInTime; }
		}

		IList<MidiMessage> messages => player.messages;
		MidiMusic music => player.music;

		public int GetTotalPlayTimeMilliseconds ()
		{
			return MidiMusic.GetTotalPlayTimeMilliseconds (messages, music.DeltaTimeSpec);
		}

		public event MidiEventAction EventReceived {
			add { player.EventReceived += value; }
			remove { player.EventReceived -= value; }
		}

		public virtual void Dispose ()
		{
			player.Stop ();
			if (should_dispose_output)
				output.Dispose ();
		}

		public void StartLoop ()
		{
			sync_player_task = Task.Run (() => { player.PlayerLoop (); });
		}

		public void PlayAsync ()
		{
			switch (State) {
			case PlayerState.Playing:
				return; // do nothing
			case PlayerState.Paused:
				player.Play ();
				return;
			case PlayerState.Stopped:
			        if (sync_player_task == null || sync_player_task.Status != TaskStatus.Running)
					StartLoop ();
				player.Play ();
				return;
			}
		}

		public void PauseAsync ()
		{
			switch (State) {
			case PlayerState.Playing:
				player.Pause ();
				return;
			default: // do nothing
				return;
			}
		}

		public void Stop ()
		{
			switch (State) {
			case PlayerState.Paused:
			case PlayerState.Playing:
				player.Stop ();
				break;
			}
		}

		public void SeekAsync (int ticks)
		{
			player.Seek (null, ticks);
		}

		public void SetChannelMask (bool [] channelMask)
		{
			if (channelMask != null && channelMask.Length != 16)
				throw new ArgumentException ("Unexpected length of channelMask array; it must be an array of 16 elements.");
			channel_mask = channelMask;
			// additionally send all sound off for the muted channels.
			for (int ch = 0; ch < channelMask.Length; ch++)
				if (channelMask [ch])
					output.Send (new byte[] {(byte) (0xB0 + ch), 120, 0}, 0, 3, 0);
		}
	}

	interface ISeekProcessor
	{
		SeekFilterResult FilterMessage (MidiMessage message);
	}

	enum SeekFilterResult
	{
		Pass,
		Block,
		PassAndTerminate,
		BlockAndTerminate,
	}
	
	class SimpleSeekProcessor : ISeekProcessor
	{
		public SimpleSeekProcessor (int ticks)
		{
			this.seek_to = ticks;
		}

		private int seek_to, current;
		public SeekFilterResult FilterMessage (MidiMessage message)
		{
			current += message.DeltaTime;
			if (current >= seek_to)
				return SeekFilterResult.PassAndTerminate;
			switch (message.Event.EventType) {
			case MidiEvent.NoteOn:
			case MidiEvent.NoteOff:
				return SeekFilterResult.Block;
			}
			return SeekFilterResult.Pass;
		}
	}
}

