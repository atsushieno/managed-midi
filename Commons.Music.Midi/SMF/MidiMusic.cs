using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Commons.Music.Midi
{ 
	public class MidiMusic
	{
	#region static members

	public static MidiMusic Read(Stream stream)
	{
		var r = new SmfReader();
		r.Read(stream);
		return r.Music;
	}

	#endregion

	List<MidiTrack> tracks = new List<MidiTrack>();

	public MidiMusic()
	{
		Format = 1;
	}

	public short DeltaTimeSpec { get; set; }

	public byte Format { get; set; }

	public void AddTrack(MidiTrack track)
	{
		this.tracks.Add(track);
	}

	public IList<MidiTrack> Tracks
	{
		get { return tracks; }
	}

	public IEnumerable<MidiMessage> GetMetaEventsOfType(byte metaType)
	{
		if (Format != 0)
			return SmfTrackMerger.Merge(this).GetMetaEventsOfType(metaType);
		return GetMetaEventsOfType(tracks[0].Messages, metaType);
	}

	public static IEnumerable<MidiMessage> GetMetaEventsOfType(IEnumerable<MidiMessage> messages, byte metaType)
	{
		int v = 0;
		foreach (var m in messages)
		{
			v += m.DeltaTime;
			if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == metaType)
				yield return new MidiMessage(v, m.Event);
		}
	}

	public int GetTotalTicks()
	{
		if (Format != 0)
			return SmfTrackMerger.Merge(this).GetTotalTicks();
		return Tracks[0].Messages.Sum(m => m.DeltaTime);
	}

	public int GetTotalPlayTimeMilliseconds()
	{
		if (Format != 0)
			return SmfTrackMerger.Merge(this).GetTotalPlayTimeMilliseconds();
		return GetTotalPlayTimeMilliseconds(Tracks[0].Messages, DeltaTimeSpec);
	}

	public int GetTimePositionInMillisecondsForTick(int ticks)
	{
		if (Format != 0)
			return SmfTrackMerger.Merge(this).GetTimePositionInMillisecondsForTick(ticks);
		return GetPlayTimeMillisecondsAtTick(Tracks[0].Messages, ticks, DeltaTimeSpec);
	}

	public static int GetTotalPlayTimeMilliseconds(IList<MidiMessage> messages, int deltaTimeSpec)
	{
		return GetPlayTimeMillisecondsAtTick(messages, messages.Sum(m => m.DeltaTime), deltaTimeSpec);
	}

	public static int GetPlayTimeMillisecondsAtTick(IList<MidiMessage> messages, int ticks, int deltaTimeSpec)
	{
		if (deltaTimeSpec < 0)
			throw new NotSupportedException("non-tick based DeltaTime");
		else
		{
			int tempo = MidiMetaType.DefaultTempo;
			int t = 0;
			double v = 0;
			foreach (var m in messages)
			{
				var deltaTime = t + m.DeltaTime < ticks ? m.DeltaTime : ticks - t;
				v += (double)tempo / 1000 * deltaTime / deltaTimeSpec;
				if (deltaTime != m.DeltaTime)
					break;
				t += m.DeltaTime;
				if (m.Event.EventType == MidiEvent.Meta && m.Event.Msb == MidiMetaType.Tempo)
					tempo = MidiMetaType.GetTempo(m.Event.ExtraData, m.Event.ExtraDataOffset);
			}
			return (int)v;
		}
	}
}
}
