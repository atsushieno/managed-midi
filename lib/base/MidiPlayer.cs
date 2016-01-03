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
		FastForward,
		Rewind,
		Loading
	}

	interface IMidiPlayerStatus
	{
		PlayerState State { get; }
		int Tempo { get; }
		int PlayDeltaTime { get; }
		TimeSpan PositionInTime { get; }
		int GetTotalPlayTimeMilliseconds ();
	}

	// Player implementation. Plays a MIDI song synchronously.
	public class MidiSyncPlayer : IDisposable, IMidiPlayerStatus
	{
		public MidiSyncPlayer (SmfMusic music)
			: this (music, new SimpleMidiTimeManager ())
		{
		}

		public MidiSyncPlayer (SmfMusic music, IMidiTimeManager timeManager)
		{
			if (music == null)
				throw new ArgumentNullException ("music");
			time_manager = timeManager;

			this.music = music;
			messages = SmfTrackMerger.Merge (music).Tracks [0].Messages;
			state = PlayerState.Stopped;
		}

		public event Action Finished;

		SmfMusic music;
		IList<SmfMessage> messages;
		ManualResetEvent pause_handle = new ManualResetEvent (false);
		PlayerState state;
		bool do_pause, do_stop;
		IMidiTimeManager time_manager;
		
		public PlayerState State {
			get { return state; }
		}
		public int PlayDeltaTime { get; set; }
		public TimeSpan PositionInTime {
			get { return GetTimerOffsetWithTempoRatio () + playtime_delta; }
		}
		public int Tempo {
			get { return current_tempo; }
		}
		// You can break the data at your own risk but I take performance precedence.
		public byte [] TimeSignature {
			get { return current_time_signature; }
		}

		public double TempoChangeRatio {
			get { return tempo_ratio; }
			set {
				playtime_delta += GetTimerOffsetWithTempoRatio ();
				timer_resumed = DateTime.Now;
				tempo_ratio = value;
			}
		}
		public int GetTotalPlayTimeMilliseconds ()
		{
			return SmfMusic.GetTotalPlayTimeMilliseconds (messages, music.DeltaTimeSpec);
		}
		
		TimeSpan GetTimerOffsetWithTempoRatio ()
		{
			switch (state) {
			case PlayerState.Playing:
			case PlayerState.FastForward:
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
				OnEvent (new SmfEvent ((byte) (i + 0xB0), 0x79, 0, null));
		}

		void Mute ()
		{
			for (int i = 0; i < 16; i++)
				OnEvent (new SmfEvent ((byte) (i + 0xB0), 0x78, 0, null));
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
			{
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
					if (Finished != null)
						Finished ();
				event_idx = 0;
			}
		}

		int current_tempo = SmfMetaType.DefaultTempo;
		byte [] current_time_signature = new byte [4];
		double tempo_ratio = 1.0;
		DateTime timer_resumed;
		TimeSpan playtime_delta;

		int GetDeltaTimeInMilliseconds (int deltaTime)
		{
			if (music.DeltaTimeSpec < 0)
				throw new NotSupportedException ("SMPTe-basd delta time is not implemented yet");
			return (int) (current_tempo / 1000 * deltaTime / music.DeltaTimeSpec / tempo_ratio);
		}

		string ToBinHexString (byte [] bytes)
		{
			string s = "";
			foreach (byte b in bytes)
				s += String.Format ("{0:X02} ", b);
			return s;
		}

		public virtual void HandleEvent (SmfMessage m)
		{
			if (m.DeltaTime != 0) {
				var ms = GetDeltaTimeInMilliseconds (m.DeltaTime);
				time_manager.AdvanceBy (ms);
			}
			if (m.Event.StatusByte == 0xFF) {
				if (m.Event.Msb == SmfMetaType.Tempo)
					current_tempo = SmfMetaType.GetTempo (m.Event.Data);
				else if (m.Event.Msb == SmfMetaType.TimeSignature && m.Event.Data.Length == 4)
					Array.Copy (m.Event.Data, current_time_signature, 4);
			}

			OnEvent (m.Event);
			PlayDeltaTime += m.DeltaTime;
		}

		public MidiEventAction EventReceived;

		protected virtual void OnEvent (SmfEvent m)
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
			}
		}
	}

	// Provides asynchronous player control.
	public class MidiPlayer : IDisposable, IMidiPlayerStatus
	{
		MidiSyncPlayer player;
		Task sync_player_task;

		public MidiPlayer (SmfMusic music)
			: this (music, MidiAccessManager.Empty)
		{
		}

		public MidiPlayer (SmfMusic music, IMidiAccess access)
			: this (music, access, new SimpleMidiTimeManager ())
		{
		}

		public MidiPlayer (SmfMusic music, IMidiOutput output)
			: this (music, output, new SimpleMidiTimeManager ())
		{
		}

		public MidiPlayer (SmfMusic music, IMidiTimeManager timeManager)
			: this (music, MidiAccessManager.Empty, timeManager)
		{
		}

		public MidiPlayer (SmfMusic music, IMidiAccess access, IMidiTimeManager timeManager)
			: this (music, access.OpenOutputAsync (access.Outputs.First ().Id).Result, timeManager)
		{
			should_dispose_output = true;
		}

		public MidiPlayer (SmfMusic music, IMidiOutput output, IMidiTimeManager timeManager)
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
				case SmfEvent.SysEx1:
				case SmfEvent.SysEx2:
					if (buffer.Length <= m.Data.Length)
						buffer = new byte [buffer.Length * 2];
					buffer [0] = m.StatusByte;
					Array.Copy (m.Data, 0, buffer, 1, m.Data.Length);
					output.SendAsync (buffer, 0, m.Data.Length + 1, 0);
					break;
				case SmfEvent.Meta:
					// do nothing.
					break;
				default:
					var size = SmfEvent.FixedDataSize (m.StatusByte);
					buffer [0] = m.StatusByte;
					buffer [1] = m.Msb;
					buffer [2] = m.Lsb;
					output.SendAsync (buffer, 0, size + 1, 0);
					break;
				}
			};
		}

		IMidiOutput output;
		bool should_dispose_output;
		byte [] buffer = new byte [0x100];

		public event Action Finished {
			add { player.Finished += value; }
			remove { player.Finished -= value; }
		}

		public PlayerState State {
			get { return player.State; }
		}

		public double TempoChangeRatio {
			get { return player.TempoChangeRatio; }
			set { player.TempoChangeRatio = value; }
		}

		public int Tempo {
			get { return player.Tempo; }
		}
		
		public int Bpm {
			get { return (int) (60.0 / Tempo * 1000000.0); }
		}
		
		public byte [] TimeSignature {
			get { return player.TimeSignature; }
		}

		public int PlayDeltaTime {
			get { return player.PlayDeltaTime; }
		}
		
		public TimeSpan PositionInTime {
			get { return player.PositionInTime; }
		}

		public int GetTotalPlayTimeMilliseconds ()
		{
			return player.GetTotalPlayTimeMilliseconds ();
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
	}
}

