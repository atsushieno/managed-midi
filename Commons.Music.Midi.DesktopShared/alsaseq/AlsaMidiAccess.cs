using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commons.Music.Midi;
using AlsaSharp;

namespace Commons.Music.Midi.Alsa {
	public class AlsaMidiAccess : IMidiAccess2 {

		class AlsaMidiAccessExtensionManager : MidiAccessExtensionManager
		{
			AlsaMidiPortCreatorExtension port_creator;

			public AlsaMidiAccessExtensionManager (AlsaMidiAccess access)
			{
				Access = access;
				port_creator = new AlsaMidiPortCreatorExtension (this);
			}
			
			public AlsaMidiAccess Access { get; private set; }

			public override T GetInstance<T> ()
			{
				if (typeof (T) == typeof (MidiPortCreatorExtension))
					return (T) (object) port_creator;
				return null;
			}
		}

		class AlsaMidiPortCreatorExtension : MidiPortCreatorExtension
		{
			AlsaMidiAccessExtensionManager manager;
			
			public AlsaMidiPortCreatorExtension (AlsaMidiAccessExtensionManager extensionManager)
			{
				manager = extensionManager;
			}

			public override IMidiOutput CreateVirtualInputSender (PortCreatorContext context)
			{
				var seq = new AlsaSequencer (AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
				var portNumber = seq.CreateSimplePort (
					context.PortName ?? "managed-midi virtual in",
					AlsaMidiAccess.virtual_input_connected_cap,
					AlsaMidiAccess.midi_port_type);
				seq.SetClientName (context.ApplicationName ?? "managed-midi input port creator");
				var port = seq.GetPort (seq.CurrentClientId, portNumber);
				var details = new AlsaMidiPortDetails (port);
				SendDelegate send = (buffer, start, length, timestamp) =>
					seq.Send (portNumber, buffer, start, length);
				return new SimpleVirtualMidiOutput (details, () => seq.DeleteSimplePort (portNumber)) { OnSend = send };
			}

			public override IMidiInput CreateVirtualOutputReceiver (PortCreatorContext context)
			{
				var seq = new AlsaSequencer (AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
				var portNumber = seq.CreateSimplePort (
					context.PortName ?? "managed-midi virtual out",
					AlsaMidiAccess.virtual_output_connected_cap,
					AlsaMidiAccess.midi_port_type);
				seq.SetClientName (context.ApplicationName ?? "managed-midi output port creator");
				var port = seq.GetPort (seq.CurrentClientId, portNumber);
				var details = new AlsaMidiPortDetails (port);
				return new SimpleVirtualMidiInput (details, () => seq.DeleteSimplePort (portNumber));
			}
		}

		const AlsaPortType midi_port_type = AlsaPortType.MidiGeneric | AlsaPortType.Application;

		AlsaSequencer system_watcher;

		public AlsaMidiAccess ()
		{
			ExtensionManager = new AlsaMidiAccessExtensionManager (this);
			system_watcher = new AlsaSequencer (AlsaIOType.Duplex, AlsaIOMode.NonBlocking);
		}

		const AlsaPortCapabilities input_requirements = AlsaPortCapabilities.Read | AlsaPortCapabilities.SubsRead;
		const AlsaPortCapabilities output_requirements = AlsaPortCapabilities.Write | AlsaPortCapabilities.SubsWrite;
		const AlsaPortCapabilities output_connected_cap = AlsaPortCapabilities.Read | AlsaPortCapabilities.NoExport;
		const AlsaPortCapabilities input_connected_cap = AlsaPortCapabilities.Write | AlsaPortCapabilities.NoExport;
		const AlsaPortCapabilities virtual_output_connected_cap = AlsaPortCapabilities.Write | AlsaPortCapabilities.SubsWrite;
		const AlsaPortCapabilities virtual_input_connected_cap = AlsaPortCapabilities.Read | AlsaPortCapabilities.SubsRead;

		public MidiAccessExtensionManager ExtensionManager { get; private set; }
		
		IEnumerable<AlsaPortInfo> EnumerateMatchingPorts (AlsaSequencer seq, AlsaPortCapabilities cap)
		{
			var cinfo = new AlsaClientInfo { Client = -1 };
			while (seq.QueryNextClient (cinfo)) {
				var pinfo = new AlsaPortInfo { Client = cinfo.Client, Port = -1 };
				while (seq.QueryNextPort (pinfo))
					if ((pinfo.PortType & midi_port_type) != 0 &&
					    (pinfo.Capabilities & cap) == cap)
						yield return pinfo.Clone ();
			}
		}

		IEnumerable<AlsaPortInfo> EnumerateAvailableInputPorts ()
		{
			return EnumerateMatchingPorts (system_watcher, input_requirements);
		}

		IEnumerable<AlsaPortInfo> EnumerateAvailableOutputPorts ()
		{
			return EnumerateMatchingPorts (system_watcher, output_requirements);
		}

		// [input device port] --> [RETURNED PORT] --> app handles messages
		AlsaPortInfo CreateInputConnectedPort (AlsaSequencer seq, AlsaPortInfo pinfo, string portName = "alsa-sharp input")
		{
			var portId = seq.CreateSimplePort (portName, input_connected_cap, midi_port_type);
			var sub = new AlsaPortSubscription ();
			sub.Destination.Client = (byte)seq.CurrentClientId;
			sub.Destination.Port = (byte)portId;
			sub.Sender.Client = (byte)pinfo.Client;
			sub.Sender.Port = (byte)pinfo.Port;
			seq.SubscribePort (sub);
			return seq.GetPort (sub.Destination.Client, sub.Destination.Port);
		}

		// app generates messages --> [RETURNED PORT] --> [output device port]
		AlsaPortInfo CreateOutputConnectedPort (AlsaSequencer seq, AlsaPortInfo pinfo, string portName = "alsa-sharp output")
		{
			var portId = seq.CreateSimplePort (portName, output_connected_cap, midi_port_type);
			var sub = new AlsaPortSubscription ();
			sub.Sender.Client = (byte)seq.CurrentClientId;
			sub.Sender.Port = (byte)portId;
			sub.Destination.Client = (byte)pinfo.Client;
			sub.Destination.Port = (byte)pinfo.Port;
			seq.SubscribePort (sub);
			return seq.GetPort (sub.Sender.Client, sub.Sender.Port);
		}

		public IEnumerable<IMidiPortDetails> Inputs => EnumerateAvailableInputPorts ().Select (p => new AlsaMidiPortDetails (p));

		public IEnumerable<IMidiPortDetails> Outputs => EnumerateAvailableOutputPorts ().Select (p => new AlsaMidiPortDetails (p));

		public event EventHandler<MidiConnectionEventArgs> StateChanged;

		public Task<IMidiInput> OpenInputAsync (string portId)
		{
			var sourcePort = (AlsaMidiPortDetails) Inputs.FirstOrDefault (p => p.Id == portId);
			if (sourcePort == null)
				throw new ArgumentException ($"Port '{portId}' does not exist.");
			var seq = new AlsaSequencer (AlsaIOType.Input, AlsaIOMode.NonBlocking);
			var appPort = CreateInputConnectedPort (seq, sourcePort.PortInfo);
			return Task.FromResult<IMidiInput> (new AlsaMidiInput (seq, new AlsaMidiPortDetails (appPort), sourcePort));
		}

		public Task<IMidiOutput> OpenOutputAsync (string portId)
		{
			var destPort = (AlsaMidiPortDetails) Outputs.FirstOrDefault (p => p.Id == portId);
			if (destPort == null)
				throw new ArgumentException ($"Port '{portId}' does not exist.");
			var seq = new AlsaSequencer (AlsaIOType.Output, AlsaIOMode.None);
			var appPort = CreateOutputConnectedPort (seq, destPort.PortInfo);
			return Task.FromResult<IMidiOutput> (new AlsaMidiOutput (seq, new AlsaMidiPortDetails (appPort), destPort));
		}
	}

	[Obsolete ("It will vanish from public API surface")]
	public class AlsaMidiPortDetails : IMidiPortDetails
	{
		AlsaPortInfo port;

		public AlsaMidiPortDetails (AlsaPortInfo port)
		{
			this.port = port;
		}

		internal AlsaPortInfo PortInfo => port;

		public string Id => port.Id;

		public string Manufacturer => port.Manufacturer;

		public string Name => port.Name;

		public string Version => port.Version;
	}

	[Obsolete ("It will vanish from public API surface")]
	public class AlsaMidiInput : IMidiInput {
		AlsaSequencer seq;
		AlsaMidiPortDetails port, source_port;

		public AlsaMidiInput (AlsaSequencer seq, AlsaMidiPortDetails appPort, AlsaMidiPortDetails sourcePort)
		{
			this.seq = seq;
			this.port = appPort;
			this.source_port = sourcePort;
			byte [] buffer = new byte [0x200];
			seq.StartListening (port.PortInfo.Port, buffer, (buf, start, len) => {
				var args = new MidiReceivedEventArgs () { Data = buf, Start = start, Length = len, Timestamp = 0 };
				MessageReceived (this, args);
			});
		}

		public IMidiPortDetails Details => source_port;

		public MidiPortConnectionState Connection { get; private set; }

		public event EventHandler<MidiReceivedEventArgs> MessageReceived;

		public Task CloseAsync ()
		{
			Dispose ();
			return Task.FromResult (string.Empty);
		}

		public void Dispose ()
		{
			// unsubscribe the app port from the MIDI input, and then delete the port.
			var q = new AlsaSubscriptionQuery { Type = AlsaSubscriptionQueryType.Write, Index = 0 };
			q.Address.Client = (byte) port.PortInfo.Client;
			q.Address.Port = (byte) port.PortInfo.Port;
			if (seq.QueryPortSubscribers (q))
				seq.DisconnectTo (port.PortInfo.Port, q.Address.Client, q.Address.Port);
			seq.DeleteSimplePort (port.PortInfo.Port);
		}
	}

	[Obsolete ("It will vanish from public API surface")]
	public class AlsaMidiOutput : IMidiOutput {
		AlsaSequencer seq;
		AlsaMidiPortDetails port, target_port;

		public AlsaMidiOutput (AlsaSequencer seq, AlsaMidiPortDetails appPort, AlsaMidiPortDetails targetPort)
		{
			this.seq = seq;
			this.port = appPort;
			this.target_port = targetPort;
		}

		public IMidiPortDetails Details => target_port;

		public MidiPortConnectionState Connection { get; private set; }

		public Task CloseAsync ()
		{
			Dispose ();
			return Task.FromResult (string.Empty);
		}

		public void Dispose ()
		{
			// unsubscribe the app port from the MIDI output, and then delete the port.
			var q = new AlsaSubscriptionQuery { Type = AlsaSubscriptionQueryType.Read, Index = 0 };
			q.Address.Client = (byte) port.PortInfo.Client;
			q.Address.Port = (byte) port.PortInfo.Port;
			if (seq.QueryPortSubscribers (q))
				seq.DisconnectTo (port.PortInfo.Port, q.Address.Client, q.Address.Port);
			seq.DeleteSimplePort (port.PortInfo.Port);
		}

		public void Send (byte [] mevent, int offset, int length, long timestamp)
		{
			seq.Send (port.PortInfo.Port, mevent, offset, length);
		}
	}
}


