using System;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	/// <summary>
	/// Used by MidiPlayer to manage time progress.
	/// </summary>
	public interface IMidiPlayerTimeManager
	{
		void WaitBy (int addedMilliseconds);
	}
	
	public class VirtualMidiPlayerTimeManager : IMidiPlayerTimeManager, IDisposable
	{
		AutoResetEvent wait_handle = new AutoResetEvent (false);
		long total_waited_milliseconds, total_proceeded_milliseconds;
		bool should_terminate, disposed;

		public void Dispose ()
		{
			Abort ();
		}

		public void Abort ()
		{
			if (disposed)
				return;
			should_terminate = true;
			wait_handle.Set ();
			wait_handle.Dispose ();
			disposed = true;
		}
		
		public virtual void WaitBy (int addedMilliseconds)
		{
			while (!should_terminate && total_waited_milliseconds + addedMilliseconds > total_proceeded_milliseconds) {
				wait_handle.WaitOne ();
			}
			total_waited_milliseconds += addedMilliseconds;
		}

		public virtual void ProceedBy (int addedMilliseconds)
		{
			if (addedMilliseconds < 0)
				throw new ArgumentOutOfRangeException ("addedMilliseconds",
					"Argument must be non-negative integer");
			total_proceeded_milliseconds += addedMilliseconds;
			wait_handle.Set ();
		}
	}

	public class SimpleAdjustingMidiPlayerTimeManager : IMidiPlayerTimeManager
	{
		DateTime last_started = default (DateTime);
		long nominal_total_mills = 0;

		public void WaitBy (int addedMilliseconds)
		{
			if (addedMilliseconds > 0) {
				long delta = addedMilliseconds;
				if (last_started != default (DateTime)) {
					var actualTotalMills = (long) (DateTime.Now - last_started).TotalMilliseconds;
					delta -= actualTotalMills - nominal_total_mills;
				} else {
					last_started = DateTime.Now;
				}
				if (delta > 0) {
					var t = Task.Delay ((int) delta);
					t.Wait ();
				}
				nominal_total_mills += addedMilliseconds;
			}
		}
	}
}
