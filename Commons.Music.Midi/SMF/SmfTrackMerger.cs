using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	public class SmfTrackMerger
	{
		public static MidiMusic Merge(MidiMusic source)
		{
			return new SmfTrackMerger(source).GetMergedMessages();
		}

		SmfTrackMerger(MidiMusic source)
		{
			this.source = source;
		}

		MidiMusic source;

		// FIXME: it should rather be implemented to iterate all
		// tracks with index to messages, pick the track which contains
		// the nearest event and push the events into the merged queue.
		// It's simpler, and costs less by removing sort operation
		// over thousands of events.
		MidiMusic GetMergedMessages()
		{
			if (source.Format == 0)
				return source;

			IList<MidiMessage> l = new List<MidiMessage>();

			foreach (var track in source.Tracks)
			{
				int delta = 0;
				foreach (var mev in track.Messages)
				{
					delta += mev.DeltaTime;
					l.Add(new MidiMessage(delta, mev.Event));
				}
			}

			if (l.Count == 0)
				return new MidiMusic() { DeltaTimeSpec = source.DeltaTimeSpec }; // empty (why did you need to sort your song file?)

			// Usual Sort() over simple list of MIDI events does not work as expected.
			// For example, it does not always preserve event 
			// orders on the same channels when the delta time
			// of event B after event A is 0. It could be sorted
			// either as A->B or B->A.
			//
			// To resolve this issue, we have to sort "chunk"
			// of events, not all single events themselves, so
			// that order of events in the same chunk is preserved
			// i.e. [AB] at 48 and [CDE] at 0 should be sorted as
			// [CDE] [AB].

			var idxl = new List<int>(l.Count);
			idxl.Add(0);
			int prev = 0;
			for (int i = 0; i < l.Count; i++)
			{
				if (l[i].DeltaTime != prev)
				{
					idxl.Add(i);
					prev = l[i].DeltaTime;
				}
			}

			idxl.Sort(delegate (int i1, int i2) {
				return l[i1].DeltaTime - l[i2].DeltaTime;
			});

			// now build a new event list based on the sorted blocks.
			var l2 = new List<MidiMessage>(l.Count);
			int idx;
			for (int i = 0; i < idxl.Count; i++)
				for (idx = idxl[i], prev = l[idx].DeltaTime; idx < l.Count && l[idx].DeltaTime == prev; idx++)
					l2.Add(l[idx]);
			l = l2;

			// now messages should be sorted correctly.

			var waitToNext = l[0].DeltaTime;
			for (int i = 0; i < l.Count - 1; i++)
			{
				if (l[i].Event.Value != 0)
				{ // if non-dummy
					var tmp = l[i + 1].DeltaTime - l[i].DeltaTime;
					l[i] = new MidiMessage(waitToNext, l[i].Event);
					waitToNext = tmp;
				}
			}
			l[l.Count - 1] = new MidiMessage(waitToNext, l[l.Count - 1].Event);

			var m = new MidiMusic();
			m.DeltaTimeSpec = source.DeltaTimeSpec;
			m.Format = 0;
			m.Tracks.Add(new MidiTrack(l));
			return m;
		}
	}
}
