using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Music.Midi
{
	// In the future we could use default interface members, but we should target earlier frameworks in the meantime.
	public class MidiAccessExtensionManager
	{
		public virtual bool Supports<T>() where T : class => GetInstance<T>() != default(T);

		public virtual T GetInstance<T>() where T : class => null;
	}

}
