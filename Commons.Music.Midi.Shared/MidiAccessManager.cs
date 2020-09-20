using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		static MidiAccessManager ()
		{
			Default = Empty = new EmptyMidiAccess ();
			new MidiAccessManager ().InitializeDefault ();
		}

		private MidiAccessManager ()
		{
			// We need this only for that we want to use partial method!
		}

		public static IMidiAccess Default { get; private set; }
		public static IMidiAccess Empty { get; internal set; }

		partial void InitializeDefault ();
	}

	//[Obsolete ("There will be breaking change in this interface in the next API-breaking release. If you want to avoid API breakage, use IMidiAccess2 now. It will become identical to IMidiAccess2 and IMidiAccess2 will remain for a while.")]
	public interface IMidiAccess
	{
		IEnumerable<IMidiPortDetails> Inputs { get; }
		IEnumerable<IMidiPortDetails> Outputs { get; }

		Task<IMidiInput> OpenInputAsync (string portId);
		Task<IMidiOutput> OpenOutputAsync (string portId);
		[Obsolete ("This will be removed in the next API-breaking change. It is not functional at this state anyways.")]
		event EventHandler<MidiConnectionEventArgs> StateChanged;
	}
	
	#region draft API

	// In the future we could use default interface members, but we should target earlier frameworks in the meantime.
	public interface IMidiAccess2 : IMidiAccess
	{
		MidiAccessExtensionManager ExtensionManager { get; }
	}

	public class MidiAccessExtensionManager
	{
		public virtual bool Supports<T> () where T : class => GetInstance<T> () != default (T);

		public virtual T GetInstance<T> () where T : class => null;
	}

	public class MidiConnectionStateDetectorExtension
	{
		public event EventHandler<MidiConnectionEventArgs> StateChanged;
	}

	public abstract class MidiPortCreatorExtension
	{
		public abstract IMidiOutput CreateVirtualInputSender (PortCreatorContext context);
		public abstract IMidiInput CreateVirtualOutputReceiver (PortCreatorContext context);

		public delegate void SendDelegate (byte [] buffer, int index, int length, long timestamp);

		public class PortCreatorContext
		{
			public string ApplicationName { get; set; }
			public string PortName { get; set; }
			public string Manufacturer { get; set; }
			public string Version { get; set; }
		}
	}

	public abstract class SimpleVirtualMidiPort : IMidiPort
	{
		IMidiPortDetails details;
		Action on_dispose;
		MidiPortConnectionState connection;

		protected SimpleVirtualMidiPort (IMidiPortDetails details, Action onDispose)
		{
			this.details = details;
			on_dispose = onDispose;
			connection = MidiPortConnectionState.Open;
		}

		public IMidiPortDetails Details => details;
		
		public MidiPortConnectionState Connection => connection;

		public Task CloseAsync ()
		{
			return Task.Run (() => {
				if (on_dispose != null)
					on_dispose ();
				connection = MidiPortConnectionState.Closed;
			});
		}

		public void Dispose ()
		{
			CloseAsync ().Wait ();
		}
	}

	public class SimpleVirtualMidiInput : SimpleVirtualMidiPort, IMidiInput
	{
		public SimpleVirtualMidiInput (IMidiPortDetails details, Action onDispose)
			: base (details, onDispose)
		{
		}

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}

	public class SimpleVirtualMidiOutput : SimpleVirtualMidiPort, IMidiOutput
	{
		public SimpleVirtualMidiOutput (IMidiPortDetails details, Action onDispose)
		: base (details, onDispose)
		{
		}
		
		public MidiPortCreatorExtension.SendDelegate OnSend { get; set; }

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			if (OnSend != null)
				OnSend (mevent, offset, length, timestamp);
		}
	}
	
	#endregion

	public class MidiConnectionEventArgs : EventArgs
	{
		public IMidiPortDetails Port { get; private set; }
	}

	public interface IMidiPortDetails
	{
		string Id { get; }
		string Manufacturer { get; }
		string Name { get; }
		string Version { get; }
	}

	public enum MidiPortConnectionState
	{
		Open,
		Closed,
		Pending
	}

	public interface IMidiPort
	{
		IMidiPortDetails Details { get; }
		MidiPortConnectionState Connection { get; }
		Task CloseAsync ();
	}

	public interface IMidiInput : IMidiPort, IDisposable
	{
		event EventHandler<MidiReceivedEventArgs> MessageReceived;
	}

	public interface IMidiOutput : IMidiPort, IDisposable
	{
		void Send (byte [] mevent, int offset, int length, long timestamp);
	}

	public class MidiReceivedEventArgs : EventArgs
	{
		public long Timestamp { get; set; }
		public byte [] Data { get; set; }
		public int Start { get; set; }
		public int Length { get; set; }
	}

	class EmptyMidiAccess : IMidiAccess
	{
		public IEnumerable<IMidiPortDetails> Inputs
		{
			get { yield return EmptyMidiInput.Instance.Details; }
		}
		
		public IEnumerable<IMidiPortDetails> Outputs
		{
			get { yield return EmptyMidiOutput.Instance.Details; }
		}
		
		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			if (portId != EmptyMidiInput.Instance.Details.Id)
				throw new ArgumentException (string.Format ("Port ID {0} does not exist.", portId));
			return Task.FromResult<IMidiInput> (EmptyMidiInput.Instance);
		}
		
		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			if (portId != EmptyMidiOutput.Instance.Details.Id)
				throw new ArgumentException (string.Format ("Port ID {0} does not exist.", portId));
			return Task.FromResult<IMidiOutput> (EmptyMidiOutput.Instance);
		}

		#pragma warning disable 0067
		// it will never be fired.
		public event EventHandler<MidiConnectionEventArgs> StateChanged;
		#pragma warning restore 0067
	}

	abstract class EmptyMidiPort : IMidiPort
	{
		Task completed_task = Task.FromResult (false);

		public IMidiPortDetails Details
		{
			get { return CreateDetails (); }
		}
		internal abstract IMidiPortDetails CreateDetails ();

		public MidiPortConnectionState Connection { get; private set; }

		public Task CloseAsync ()
		{
			// do nothing.
			return completed_task;
		}

		public void Dispose ()
		{
		}
	}

	class EmptyMidiPortDetails : IMidiPortDetails
	{
		public EmptyMidiPortDetails (string id, string name)
		{
			Id = id;
			Manufacturer = "dummy project";
			Name = name;
			Version = "0.0";
		}

		public string Id { get; set; }
		public string Manufacturer { get; set; }
		public string Name { get; set; }
		public string Version { get; set; }
	}

	class EmptyMidiInput : EmptyMidiPort, IMidiInput
	{
		static EmptyMidiInput ()
		{
			Instance = new EmptyMidiInput ();
		}

		public static EmptyMidiInput Instance { get; private set; }

		#pragma warning disable 0067
		// will never be fired.
		public event EventHandler<MidiReceivedEventArgs> MessageReceived;
		#pragma warning restore 0067

		internal override IMidiPortDetails CreateDetails ()
		{
			return new EmptyMidiPortDetails ("dummy_in", "Dummy MIDI Input");
		}
	}

	class EmptyMidiOutput : EmptyMidiPort, IMidiOutput
	{
		Task completed_task = Task.FromResult (false);

		static EmptyMidiOutput ()
		{
			Instance = new EmptyMidiOutput ();
		}

		public static EmptyMidiOutput Instance { get; private set; }

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			// do nothing.
		}

		internal override IMidiPortDetails CreateDetails ()
		{
			return new EmptyMidiPortDetails ("dummy_out", "Dummy MIDI Output");
		}
	}
}
