using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CoreMidi;

using MIDI = CoreMidi.Midi;

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

namespace Commons.Music.Midi.CoreMidiApi
{
	public class CoreMidiAccess : IMidiAccess
	{
		IEnumerable<MidiEntity> EnumerateMidiEntities ()
		{
			var dcount = MIDI.DeviceCount;
			for (nint d = 0; d < dcount; d++) {
				var dev = MIDI.GetDevice (d);
				var ecount = dev.EntityCount;
				for (nint e = 0; e < ecount; e++)
					yield return dev.GetEntity (e);
			}
		}

		public IEnumerable<IMidiPortDetails> Inputs {
			get {
				foreach (var ent in EnumerateMidiEntities ()) {
					var scount = ent.Sources;
					for (nint s = 0; s < scount; s++)
						yield return new CoreMidiPortDetails (ent.GetSource (s));
				}
			}
		}

		public IEnumerable<IMidiPortDetails> Outputs {
			get {
				foreach (var ent in EnumerateMidiEntities ()) {
					var dcount = ent.Destinations;
					for (nint d = 0; d < dcount; d++)
						yield return new CoreMidiPortDetails (ent.GetDestination (d));
				}
			}
		}

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
				throw new InvalidOperationException($"Device specified as port {portId}) is not found.");
			return Task.FromResult((IMidiOutput) new CoreMidiOutput (details));
		}
	}

	class CoreMidiPortDetails : IMidiPortDetails
	{
		public CoreMidiPortDetails (MidiEndpoint src)
		{
			Endpoint = src;
			Id = src.EndpointName + "__" + src.Name;
			Manufacturer = src.Manufacturer;
			Name = src.DisplayName;

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
	}

	class CoreMidiInput : IMidiInput
	{
		public CoreMidiInput (CoreMidiPortDetails details)
		{
			this.details = details;
			port = new MidiClient("inputclient").CreateInputPort("inputport");
			port.ConnectSource(details.Endpoint);
			port.MessageReceived += OnMessageReceived;
		}

		CoreMidiPortDetails details;
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
					MessageReceived(sender, new MidiReceivedEventArgs() { Data = dispatch_bytes, Start = 0, Length = dispatch_bytes.Length, Timestamp = p.TimeStamp });
				}
			}
		}

		public Task CloseAsync()
		{
			port.Disconnect(details.Endpoint);
			port.Client.Dispose();
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
			port = new MidiClient("outputclient").CreateOutputPort("outputport");
		}

		CoreMidiPortDetails details;
		MidiPort port;

		public IMidiPortDetails Details => details;

		public MidiPortConnectionState Connection => throw new NotImplementedException();

		public Task CloseAsync()
		{
			port.Disconnect(details.Endpoint);
			port.Client.Dispose();
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
