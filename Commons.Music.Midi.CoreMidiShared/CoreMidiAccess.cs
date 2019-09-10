using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreMidi;

using MIDI = CoreMidi.Midi;
#if !__IOS__ && !__MACOS__
using nint = System.Int64;
#endif

#if __IOS__ || __MACOS__
namespace Commons.Music.Midi
{
	public partial class MidiAccessManager
	{
		partial void InitializeDefault ()
		{
			Default = new CoreMidiApi.CoreMidiAccess ();
		}
	}
}
#endif

namespace Commons.Music.Midi.CoreMidiApi
{
	public class CoreMidiAccess : IMidiAccess2
	{
		class CoreMidiAccessExtensionManager : MidiAccessExtensionManager
		{
			private CoreMidiPortCreatorExtension port_creator = new CoreMidiPortCreatorExtension ();
			public override T GetInstance<T> ()
			{
				if (typeof(T) == typeof(MidiPortCreatorExtension))
					return (T) (object) port_creator;
				return null;
			}
		}

		class CoreMidiPortCreatorExtension : MidiPortCreatorExtension
		{
			public override IMidiOutput CreateVirtualInputSender (PortCreatorContext context)
			{
				var nclient = new MidiClient (context.ApplicationName ?? "managed-midi virtual in");
				MidiError error;
				var portName = context.PortName ?? "managed-midi virtual in port";
				var nendpoint = nclient.CreateVirtualSource (portName, out error);
				nendpoint.Manufacturer = context.Manufacturer;
				nendpoint.DisplayName = portName;
				nendpoint.Name = portName;
				var details = new CoreMidiPortDetails (nendpoint);
				return new CoreMidiOutput (details);
			}

			public override IMidiInput CreateVirtualOutputReceiver (PortCreatorContext context)
			{
				var nclient = new MidiClient (context.ApplicationName ?? "managed-midi virtual out");
				MidiError error;
				var portName = context.PortName ?? "managed-midi virtual out port";
				var nendpoint = nclient.CreateVirtualDestination (portName, out error);
				nendpoint.Manufacturer = context.Manufacturer;
				nendpoint.DisplayName = portName;
				nendpoint.Name = portName;
				var details = new CoreMidiPortDetails (nendpoint);
				return new CoreMidiInput (details);
			}
		}
		
		public MidiAccessExtensionManager ExtensionManager { get; } = new CoreMidiAccessExtensionManager ();
		
		public IEnumerable<IMidiPortDetails> Inputs => Enumerable.Range (0, (int) MIDI.SourceCount).Select (i => (IMidiPortDetails) new CoreMidiPortDetails (MidiEndpoint.GetSource (i)));

		public IEnumerable<IMidiPortDetails> Outputs => Enumerable.Range (0, (int)MIDI.DestinationCount).Select (i => (IMidiPortDetails)new CoreMidiPortDetails (MidiEndpoint.GetDestination (i)));

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync(string portId)
		{
			var details = Inputs.Cast<CoreMidiPortDetails> ().FirstOrDefault (i => i.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"Device specified as port {portId}) is not found.");
			return Task.FromResult((IMidiInput) new CoreMidiInput (details));
		}

		public Task<IMidiOutput> OpenOutputAsync(string portId)
		{
			var details = Outputs.Cast<CoreMidiPortDetails>().FirstOrDefault(i => i.Id == portId);
			if (details == null)
				throw new InvalidOperationException($"Device specified as port {portId} is not found.");
			return Task.FromResult((IMidiOutput) new CoreMidiOutput (details));
		}
	}

	class CoreMidiPortDetails : IMidiPortDetails, IDisposable
	{
		public CoreMidiPortDetails (MidiEndpoint src)
		{
			Endpoint = src;
			Id = src.Name + "__" + src.EndpointName;
			Manufacturer = src.Manufacturer;
			Name = string.IsNullOrEmpty (src.DisplayName) ? src.Name : src.EndpointName;

			try {
				Version = src.DriverVersion.ToString ();
			} catch {
				Version = "N/A";
			}
		}

		public MidiEndpoint Endpoint { get; set; }

		public string Id { get; set; }

		public string Manufacturer { get; set; }

		public string Name { get; set; }

		public string Version { get; set; }

		public void Dispose()
		{
			Endpoint?.Dispose ();
		}
	}

	class CoreMidiInput : IMidiInput
	{
		public CoreMidiInput (CoreMidiPortDetails details)
		{
			this.details = details;
			client = new MidiClient ("inputclient");
			port = client.CreateInputPort ("inputport");
			port.ConnectSource (details.Endpoint);
			port.MessageReceived += OnMessageReceived;
		}

		CoreMidiPortDetails details;
		MidiClient client;
		MidiPort port;

		public IMidiPortDetails Details => details;

		public MidiPortConnectionState Connection => throw new NotImplementedException();

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		byte[] dispatch_bytes = new byte[100];

		void OnMessageReceived (object sender, MidiPacketsEventArgs e)
		{
			if (MessageReceived != null)
			{
				foreach (var p in e.Packets)
				{
					if (dispatch_bytes.Length < p.Length)
						dispatch_bytes = new byte[p.Length];
					Marshal.Copy(p.Bytes, dispatch_bytes, 0, p.Length);
					MessageReceived(this, new MidiReceivedEventArgs() { Data = dispatch_bytes, Start = 0, Length = p.Length, Timestamp = p.TimeStamp });
				}
			}
		}

		public Task CloseAsync()
		{
			port.Disconnect (details.Endpoint);
			port.Dispose ();
			client.Dispose ();
			details.Dispose ();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			CloseAsync().RunSynchronously();
		}
	}

	class CoreMidiOutput : IMidiOutput
	{
		public CoreMidiOutput (CoreMidiPortDetails details)
		{
			this.details = details;
			client = new MidiClient ("outputclient");
			port = client.CreateOutputPort ("outputport");
		}

		MidiClient client;
		CoreMidiPortDetails details;
		MidiPort port;

		public IMidiPortDetails Details => details;

		public MidiPortConnectionState Connection => throw new NotImplementedException();

		public Task CloseAsync()
		{
			port.Disconnect (details.Endpoint);
			port.Dispose ();
			client.Dispose ();
			details.Dispose ();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			CloseAsync().RunSynchronously();
		}

		MidiPacket[] arr = new MidiPacket[1];
		public void Send (byte[] mevent, int offset, int length, long timestamp)
		{
			unsafe {
				fixed (byte* ptr = mevent) {
					arr [0] = new MidiPacket(timestamp, (ushort)length, (IntPtr)(ptr + offset));
					port.Send (details.Endpoint, arr);
				}
			}
		}
	}
}
