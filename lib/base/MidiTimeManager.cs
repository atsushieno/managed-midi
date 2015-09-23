using System;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public interface IMidiTimeManager
	{
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
		public override void AdvanceBy (int addedMilliseconds)
		{
#if PORTABLE
			var t = Task.Delay (addedMilliseconds);
			t.Wait ();
#else
			System.Threading.Thread.Sleep (addedMilliseconds);
#endif
			base.AdvanceBy (addedMilliseconds);
		}

		/*
		public void AdvanceTo (long targetTicks)
		{
		}
		*/
	}
}
