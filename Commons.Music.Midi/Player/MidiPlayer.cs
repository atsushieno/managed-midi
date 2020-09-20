using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	
	// Provides asynchronous player control.
	public class MidiPlayer : IDisposable
	{
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

			this.music = music;
			this.output = output;

			messages = SmfTrackMerger.Merge (music).Tracks [0].Messages;
			player = new MidiEventLooper (messages, timeManager, music.DeltaTimeSpec);
			player.Starting += () => {
				// all control reset on all channels.
				for (int i = 0; i < 16; i++) {
					buffer [0] = (byte) (i + 0xB0);
					buffer [1] = 0x79;
					buffer [2] = 0;
					output.Send (buffer, 0, 3, 0);
				}
			};
			EventReceived += (m) => {
				switch (m.EventType) {
				case MidiEvent.NoteOn:
				case MidiEvent.NoteOff:
					if (channel_mask != null && channel_mask [m.Channel])
						return; // ignore messages for the masked channel.
					goto default;
				case MidiEvent.SysEx1:
				case MidiEvent.SysEx2:
					if (buffer.Length <= m.ExtraDataLength)
						buffer = new byte [buffer.Length * 2];
					buffer [0] = m.StatusByte;
					Array.Copy (m.ExtraData, m.ExtraDataOffset, buffer, 1, m.ExtraDataLength);
					output.Send (buffer, 0, m.ExtraDataLength + 1, 0);
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

		MidiEventLooper player;
		// FIXME: it is still awkward to have it here. Move it into MidiEventLooper.
		Task sync_player_task;
		IMidiOutput output;
		IList<MidiMessage> messages;
		MidiMusic music;

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
			get => player.tempo_ratio;
			set => player.tempo_ratio = value;
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
			get { return player.play_delta_time; }
		}
		
		public TimeSpan PositionInTime => TimeSpan.FromMilliseconds (music.GetTimePositionInMillisecondsForTick (PlayDeltaTime));

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

		[Obsolete ("This should not be callable externally. It will be removed in the next API-breaking update.")]
		public void StartLoop ()
		{
			sync_player_task = Task.Run (() => { player.PlayerLoop (); });
		}

		[Obsolete ("Its naming is misleading. It starts playing asynchronously, but won't return any results unlike typical async API. Use new Play() method instead")]
		public void PlayAsync () => Play ();

		public void Play ()
		{
			switch (State) {
			case PlayerState.Playing:
				return; // do nothing
			case PlayerState.Paused:
				player.Play ();
				return;
			case PlayerState.Stopped:
			        if (sync_player_task == null || sync_player_task.Status != TaskStatus.Running)
#pragma warning disable 618
					StartLoop ();
#pragma warning restore 618
				player.Play ();
				return;
			}
		}

		[Obsolete ("Its naming is misleading. It starts playing asynchronously, but won't return any results unlike typical async API. Use new Pause() method instead")]
		public void PauseAsync () => Pause ();

		public void Pause ()
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

		[Obsolete ("Its naming is misleading. It starts seeking asynchronously, but won't return any results unlike typical async API. Use new Seek() method instead")]
		public void SeekAsync (int ticks) => Seek (ticks);
		
		public void Seek (int ticks)
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

}

