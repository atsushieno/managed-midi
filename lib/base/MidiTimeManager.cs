using System;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public interface IMidiTimeManager
	{
		bool Counting { get; set; }
		//void AdvanceByTicks (long addedTicks, int currentTempo, int smfDeltaTimeSpec, double speed = 1.0);
		void AdvanceBy (int addedMilliseconds);
		//void AdvanceTo (long targetMilliseconds);

		//long TicksToMilliseconds (long ticks)
	}

	public abstract class MidiTimeManagerBase : IMidiTimeManager
	{
		public static int GetDeltaTimeInMilliseconds (int deltaTime, int currentTempo, int smfDeltaTimeSpec, double speed = 1.0)
		{
			if (smfDeltaTimeSpec < 0)
				throw new NotSupportedException ("SMPTe-basd delta time is not implemented yet");
			return (int) (currentTempo / 1000 * deltaTime / smfDeltaTimeSpec / speed);
		}

		//public virtual long TotalTicks { get; private set; }

		public virtual bool Counting { get; set; }

		public virtual void AdvanceBy (int addedMilliseconds)
		{
			if (addedMilliseconds < 0)
				throw new InvalidOperationException ("Added ticks must be non-negative.");
			//TotalTicks += addedTicks;
		}

		/*
	 	public virtual void AdvanceTo (long targetTicks)
		{
			if (targetTicks < TotalTicks)
				throw new InvalidOperationException ("target ticks must not be less than current total ticks.");
			TotalTicks = targetTicks;
		}
		*/
	}


	public class VirtualMidiTimeManager : MidiTimeManagerBase
	{
		public override void AdvanceBy (int addedMilliseconds)
		{
			base.AdvanceBy (addedMilliseconds);
		}
		//void AdvanceTo (long targetTicks);
	}

	public class SimpleMidiTimeManager : MidiTimeManagerBase
	{
		DateTime last_checked = default (DateTime);

		public override bool Counting {
			get { return base.Counting; }
			set {
				if (!value)
					last_checked = default (DateTime); // reset
				base.Counting = value;
			}
		}

		public override void AdvanceBy (int addedMilliseconds)
		{
			long delta = addedMilliseconds;
			if (last_checked != default (DateTime))
				delta = addedMilliseconds - (DateTime.Now - last_checked).Milliseconds;
			if (delta > 0) {
				var t = Task.Delay ((int) delta);
				t.Wait ();
			}
			last_checked = DateTime.Now;
			base.AdvanceBy (addedMilliseconds);
		}

		/*
		public void AdvanceTo (long targetTicks)
		{
		}
		*/
	}
}
