using Commons.Music.Midi;
using Commons.Music.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace XFTest
{
    public partial class TestPage : ContentPage
    {
        IMidiAccess _access;
        IMidiPortDetails _device = null;
        IMidiOutput _synthesizer = null;
        ObservableCollection<string> _messages;
        private Dictionary<byte, string> messageTypes;
        private byte currentMessageType;

        public TestPage()
        {
            InitializeComponent();
            _access = MidiAccessManager.Default;
            _messages = new ObservableCollection<string>();
            PopulateMessageTypes();
        }
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            InputDevices.ItemsSource = _access.Inputs;
            OutputDevices.ItemsSource = _access.Outputs;
            Messages.ItemsSource = _messages;
        }

        /// <summary>
        /// Reset all input fields
        /// </summary>
        /// <param name="resetMessageType">If true, reset message type list as well</param>
        private void ResetMessageTypeAndParameters(bool resetMessageType)
        {
            // If the flag is set, reset the message type list as well
            if (resetMessageType)
            {
                this.MessageType.SelectedIndex = -1;
                this.currentMessageType = 0;
            }

            // Ensure the message type list and reset button are enabled
            this.MessageType.IsEnabled = true;
            this.ResetButton.IsEnabled = true;

            // Reset selections on Parameters
            this.Parameter1.SelectedIndex = -1;
            this.Parameter2.SelectedIndex = -1;
            this.Parameter3.SelectedIndex = -1;

            // New selection values will cause Parameter boxes to be hidden and disabled
            UpdateParameterList1();
            UpdateParameterList2();
            UpdateParameterList3();

            // Disable send button & hide/clear the SysEx buffer text
            this.SendButton.IsEnabled = false;
            this.RawBufferHeader.IsVisible = false;
            this.SysExMessageContent.Text = "";
            this.SysExMessageContent.IsVisible = false;
        }
        private void PopulateMessageTypes()
        {
            // Build the list of available MIDI messages for reverse lookup later
            this.messageTypes = new Dictionary<byte, string>();
            this.messageTypes.Add(MidiEvent.ActiveSense, "Active Sensing");
            this.messageTypes.Add(MidiEvent.CAf, "Channel Pressure");
            this.messageTypes.Add(MidiEvent.MidiContinue, "Continue");
            this.messageTypes.Add(MidiEvent.CC, "Control Change");
            this.messageTypes.Add(MidiEvent.MtcQuarterFrame, "MIDI Time Code");
            this.messageTypes.Add(MidiEvent.NoteOff, "Note Off");
            this.messageTypes.Add(MidiEvent.NoteOn, "Note On");
            this.messageTypes.Add(MidiEvent.Pitch, "Pitch Bend Change");
            this.messageTypes.Add(MidiEvent.PAf, "Polyphonic Key Pressure");
            this.messageTypes.Add(MidiEvent.Program, "Program Change");
            this.messageTypes.Add(MidiEvent.SongPositionPointer, "Song Position Pointer");
            this.messageTypes.Add(MidiEvent.SongSelect, "Song Select");
            this.messageTypes.Add(MidiEvent.MidiStart, "Start");
            this.messageTypes.Add(MidiEvent.MidiStop, "Stop");
            this.messageTypes.Add(MidiEvent.SysEx1, "System Exclusive");
            this.messageTypes.Add(MidiEvent.Reset, "System Reset");
            this.messageTypes.Add(MidiEvent.MidiClock, "Timing Clock");
            this.messageTypes.Add(MidiEvent.TuneRequest, "Tune Request");

            // Start with a clean slate
            MessageType.Items.Clear();

            // Add the message types to the list
            foreach (var messageType in this.messageTypes)
            {
                MessageType.Items.Add(messageType.Value);
            }
        }
        /// <summary>
        /// Construct a MIDI message possibly with additional Parameters,
        /// depending on the type of message
        /// </summary>
        /// <param name="sender">Element that fired the event</param>
        /// <param name="e">Event arguments</param>
        private async void MessageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Find the index of the user's choice
            int messageTypeSelectedIndex = this.MessageType.SelectedIndex;

            // Return if reset
            if (messageTypeSelectedIndex == -1)
            {
                return;
            }

            // Clear the UI
            ResetMessageTypeAndParameters(false);

            // Find the key by index; that's our message type
            int count = 0;
            foreach (var messageType in messageTypes)
            {
                if (messageTypeSelectedIndex == count)
                {
                    this.currentMessageType = messageType.Key;
                    break;
                }
                count++;
            }

            // Some MIDI message types don't need additional Parameters
            // For them, show the Send button as soon as user selects message type from the list
            switch (this.currentMessageType)
            {
                // SysEx messages need to be in a particular format
                case MidiEvent.SysEx1:
                    this.RawBufferHeader.IsVisible = true;
                    this.SysExMessageContent.IsVisible = true;
                    // Provide start (0xF0) and end (0xF7) of SysEx values
                    SysExMessageContent.Text = "F0 F7";
                    // Let the user know the expected format of the message
                    await DisplayAlert("", "Expecting a string of format 'NN NN NN NN....', where NN is a byte in hex", "Ok");
                    this.SendButton.IsEnabled = true;
                    break;

                // These messages do not need additional Parameters
                case MidiEvent.ActiveSense:
                case MidiEvent.MidiContinue:
                case MidiEvent.MidiStart:
                case MidiEvent.MidiStop:
                case MidiEvent.Reset:
                case MidiEvent.MidiClock:
                case MidiEvent.TuneRequest:
                    this.SendButton.IsEnabled = true;
                    break;

                default:
                    this.SendButton.IsEnabled = false;
                    break;
            }

            // Update the first Parameter list depending on the MIDI message type
            // If no further Parameters are required, the list is emptied and hidden
            UpdateParameterList1();
        }
        /// <summary>
        /// Helper function to populate a dropdown lists with options
        /// </summary>
        /// <param name="list">The Parameter list to populate</param>
        /// <param name="numberOfOptions">Number of options in the list</param>
        /// <param name="listName">The header to display to the user</param>
        private void PopulateParameterList(Picker list, int numberOfOptions)
        {
            // Start with a clean slate
            list.Items.Clear();

            // Add the options to the list
            for (int i = 0; i < numberOfOptions; i++)
            {
                list.Items.Add(string.Format("{0}", i));
            }

            // Show the list, so that the user can make the next choice
            list.IsEnabled = true;
            list.IsVisible = true;
            list.SelectedIndex = 0;
        }
        /// <summary>
        /// For MIDI message types that need the first Parameter, populate the list
        /// based on the message type. For message types that don't need the first
        /// Parameter, empty and hide it
        /// </summary>
        private async void UpdateParameterList1()
        {
            // The first Parameter is different for different message types
            switch (this.currentMessageType)
            {
                // For message types that require a first Parameter...
                case MidiEvent.NoteOff:
                case MidiEvent.NoteOn:
                case MidiEvent.PAf:
                case MidiEvent.CC:
                case MidiEvent.Program:
                case MidiEvent.CAf:
                case MidiEvent.Pitch:
                    // This list is for Channels, of which there are 16
                    Parameter1_Header.Text = "Channel";
                    Parameter1_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter1, 16);
                    break;

                case MidiEvent.MtcQuarterFrame:
                    // This list is for further Message Types, of which there are 8
                    Parameter1_Header.Text = "Message Type";
                    Parameter1_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter1, 8);
                    break;

                case MidiEvent.SongPositionPointer:
                    // This list is for Beats, of which there are 16384
                    Parameter1_Header.Text = "Beats";
                    Parameter1_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter1, 16384);
                    break;

                case MidiEvent.SongSelect:
                    // This list is for Songs, of which there are 128
                    Parameter1_Header.Text = "Song";
                    Parameter1_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter1, 128);
                    break;

                case MidiEvent.SysEx1:
                    // Start with a clean slate
                    this.Parameter1.Items.Clear();

                    // Hide the first Parameter
                    this.Parameter1_Header.Text = "";
                    this.Parameter1.IsEnabled = false;
                    this.Parameter1.IsVisible = true;
                    await DisplayAlert("", "Please edit the message in the textbox by clicking on 'F0 F7'", "Ok");
                    break;

                default:
                    // Start with a clean slate
                    this.Parameter1.Items.Clear();

                    // Hide the first Parameter
                    this.Parameter1_Header.Text = "";
                    this.Parameter1.IsEnabled = false;
                    this.Parameter1.IsVisible = false;
                    break;
            }
        }

        /// <summary>
        /// React to Parameter1 selection change as appropriate
        /// </summary>
        /// <param name="sender">Element that fired the event</param>
        /// <param name="e">Event arguments</param>
        private void Parameter1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Find the index of the user's choice
            int Parameter1SelectedIndex = this.Parameter1.SelectedIndex;

            // Some MIDI message types don't need additional Parameters past Parameter 1
            // For them, show the Send button as soon as user selects Parameter 1 value from the list
            switch (this.currentMessageType)
            {
                case MidiEvent.SongPositionPointer:
                case MidiEvent.SongSelect:

                    if (Parameter1SelectedIndex != -1)
                    {
                        this.SendButton.IsEnabled = true;
                    }
                    break;

                default:
                    this.SendButton.IsEnabled = false;
                    break;
            }

            // Update the second Parameter list depending on the first Parameter selection
            // If no further Parameters are required, the list is emptied and hidden
            UpdateParameterList2();
        }
        /// <summary>
        /// React to Parameter2 selection change as appropriate
        /// </summary>
        /// <param name="sender">Element that fired the event</param>
        /// <param name="e">Event arguments</param>
        private void Parameter2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Find the index of the user's choice
            int Parameter2SelectedIndex = this.Parameter2.SelectedIndex;

            // Some MIDI message types don't need additional Parameters past Parameter 2
            // For them, show the Send button as soon as user selects Parameter 2 value from the list
            switch (this.currentMessageType)
            {
                case MidiEvent.Program:
                case MidiEvent.CAf:
                case MidiEvent.Pitch:
                case MidiEvent.MtcQuarterFrame:

                    if (Parameter2SelectedIndex != -1)
                    {
                        this.SendButton.IsEnabled = true;
                    }
                    break;

                default:
                    this.SendButton.IsEnabled = false;
                    break;
            }

            // Update the third Parameter list depending on the second Parameter selection
            // If no further Parameters are required, the list is emptied and hidden
            UpdateParameterList3();
        }

        /// <summary>
        /// For MIDI message types that need the second Parameter, populate the list
        /// based on the message type. For message types that don't need the second
        /// Parameter, empty and hide it
        /// </summary>
        private void UpdateParameterList2()
        {
            // Do not proceed if Parameter 1 is not chosen
            if (this.Parameter1.SelectedIndex == -1)
            {
                this.Parameter2.Items.Clear();
                this.Parameter2_Header.Text = "";
                this.Parameter2.IsEnabled = false;
                this.Parameter2.IsVisible = false;

                return;
            }

            switch (this.currentMessageType)
            {
                case MidiEvent.NoteOff:
                case MidiEvent.NoteOn:
                case MidiEvent.PAf:
                    // This list is for Notes, of which there are 128
                    Parameter2_Header.Text = "Note";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 128);
                    break;

                case MidiEvent.CC:
                    // This list is for Controllers, of which there are 128
                    Parameter2_Header.Text = "Controller";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 128);
                    break;

                case MidiEvent.Program:
                    // This list is for Program Numbers, of which there are 128
                    Parameter2_Header.Text = "Program Number";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 128);
                    break;

                case MidiEvent.CAf:
                    // This list is for Pressure Values, of which there are 128
                    Parameter2_Header.Text = "Pressure Value";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 128);
                    break;

                case MidiEvent.Pitch:
                    // This list is for Pitch Bend Values, of which there are 16384
                    Parameter2_Header.Text = "Pitch Bend Value";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 16384);
                    break;

                case MidiEvent.MtcQuarterFrame:
                    // This list is for Values, of which there are 16
                    Parameter2_Header.Text = "Value";
                    Parameter2_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter2, 16);
                    break;

                default:
                    // Start with a clean slate
                    this.Parameter2.Items.Clear();

                    // Hide the first Parameter
                    this.Parameter2_Header.Text = "";
                    this.Parameter2.IsEnabled = false;
                    this.Parameter2.IsVisible = false;
                    break;
            }
        }
        /// <summary>
        /// React to Parameter3 selection change as appropriate
        /// </summary>
        /// <param name="sender">Element that fired the event</param>
        /// <param name="e">Event arguments</param>
        private void Parameter3_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Find the index of the user's choice
            int Parameter3SelectedIndex = this.Parameter3.SelectedIndex;

            // The last set of MIDI message types don't need additional Parameters
            // For them, show the Send button as soon as user selects Parameter 3 value from the list
            // Set default to disable Send button for any message types that fall through
            switch (this.currentMessageType)
            {
                case MidiEvent.NoteOff:
                case MidiEvent.NoteOn:
                case MidiEvent.PAf:
                case MidiEvent.CC:

                    if (Parameter3SelectedIndex != -1)
                    {
                        this.SendButton.IsEnabled = true;
                    }
                    break;

                default:
                    this.SendButton.IsEnabled = false;
                    break;
            }
        }
        /// <summary>
        /// For MIDI message types that need the third Parameter, populate the list
        /// based on the message type. For message types that don't need the third
        /// Parameter, empty and hide it
        /// </summary>
        private void UpdateParameterList3()
        {
            // Do not proceed if Parameter 2 is not chosen
            if (this.Parameter2.SelectedIndex == -1)
            {
                this.Parameter3.Items.Clear();
                this.Parameter3_Header.Text = "";
                this.Parameter3.IsEnabled = false;
                this.Parameter3.IsVisible = false;

                return;
            }

            switch (this.currentMessageType)
            {
                case MidiEvent.NoteOff:
                case MidiEvent.NoteOn:
                    // This list is for Velocity Values, of which there are 128
                    Parameter3_Header.Text = "Velocity";
                    Parameter3_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter3, 128);
                    break;

                case MidiEvent.PAf:
                    // This list is for Pressure Values, of which there are 128
                    Parameter3_Header.Text = "Pressure";
                    Parameter3_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter3, 128);
                    break;

                case MidiEvent.CC:
                    // This list is for Values, of which there are 128
                    Parameter3_Header.Text = "Value";
                    Parameter3_Header.IsVisible = true;
                    PopulateParameterList(this.Parameter3, 128);
                    break;

                default:
                    // Start with a clean slate
                    this.Parameter3.Items.Clear();

                    // Hide the first Parameter
                    this.Parameter3_Header.Text = "";
                    this.Parameter3.IsEnabled = false;
                    this.Parameter3.IsVisible = false;
                    break;
            }
        }
        private void OnInputDeviceSelected(object sender, SelectedItemChangedEventArgs args)
        {
            IMidiPortDetails device = args.SelectedItem as IMidiPortDetails;
            if (device != null)
            {
                _messages.Clear();

                var input = _access.OpenInputAsync(device.Id).Result;
                _messages.Add("Using " + device.Id);
                input.MessageReceived += (obj, e) =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            _messages.Add($"{e.Timestamp} {e.Start} {e.Length} {e.Data[0].ToString("X")}");
                            Messages.ScrollTo(_messages.Last(), ScrollToPosition.MakeVisible, true);
                            if (_synthesizer != null)
                            {
                                _synthesizer.Send(e.Data, e.Start, e.Length, e.Timestamp);
                            }
                        }
                        catch (Exception exp)
                        {
                            Debug.WriteLine(exp);
                        }
                    });
                };
            }
        }

        private async void OnOutputItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _device = e.SelectedItem as IMidiPortDetails;
            if (_device != null)
            {
                this.MessageType.IsEnabled = true;
                this.ResetButton.IsEnabled = true;

                _synthesizer = await _access.OpenOutputAsync(_device.Id);
            }
        }

        /// <summary>
        /// Create a new MIDI message based on the message type and Parameter(s) values,
        /// and send it to the chosen output device
        /// </summary>
        /// <param name="sender">Element that fired the event</param>
        /// <param name="e">Event arguments</param>
        private void SendButton_Clicked(object sender, EventArgs e)
        {
            MidiMessage midiMessageToSend = null;

            switch (this.currentMessageType)
            {
                case MidiEvent.NoteOff:
                    midiMessageToSend = new MidiNoteOffMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem), Convert.ToByte(this.Parameter3.SelectedItem));
                    break;
                case MidiEvent.NoteOn:
                    midiMessageToSend = new MidiNoteOnMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem), Convert.ToByte(this.Parameter3.SelectedItem));
                    break;
                case MidiEvent.PAf:
                    midiMessageToSend = new MidiPolyphonicKeyPressureMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem), Convert.ToByte(this.Parameter3.SelectedItem));
                    break;
                case MidiEvent.CC:
                    midiMessageToSend = new MidiControlChangeMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem), Convert.ToByte(this.Parameter3.SelectedItem));
                    break;
                case MidiEvent.Program:
                    midiMessageToSend = new MidiProgramChangeMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem));
                    break;
                case MidiEvent.CAf:
                    midiMessageToSend = new MidiChannelPressureMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem));
                    break;
                case MidiEvent.Pitch:
                    midiMessageToSend = new MidiPitchBendChangeMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToUInt16(this.Parameter2.SelectedItem));
                    break;
                case MidiEvent.SysEx1:
                    var dataWriter = new List<byte>();
                    var sysExMessage = this.SysExMessageContent.Text;
                    var sysExMessageLength = sysExMessage.Length;

                    // Do not send a blank SysEx message
                    if (sysExMessageLength == 0)
                    {
                        return;
                    }

                    // SysEx messages are two characters long with 1-character space in between them
                    // So we add 1 to the message length, so that it is perfectly divisible by 3
                    // The loop count tracks the number of individual message pieces
                    int loopCount = (sysExMessageLength + 1) / 3;

                    // Expecting a string of format "F0 NN NN NN NN.... F7", where NN is a byte in hex
                    for (int i = 0; i < loopCount; i++)
                    {
                        var messageString = sysExMessage.Substring(3 * i, 2);
                        var messageByte = Convert.ToByte(messageString, 16);
                        dataWriter.Add(messageByte);
                    }
                    midiMessageToSend = new MidiSystemExclusiveMessage(dataWriter.ToArray());
                    break;
                case MidiEvent.MtcQuarterFrame:
                    midiMessageToSend = new MidiTimeCodeMessage(Convert.ToByte(this.Parameter1.SelectedItem), Convert.ToByte(this.Parameter2.SelectedItem));
                    break;
                case MidiEvent.SongPositionPointer:
                    midiMessageToSend = new MidiSongPositionPointerMessage(Convert.ToUInt16(this.Parameter1.SelectedItem));
                    break;
                case MidiEvent.SongSelect:
                    midiMessageToSend = new MidiSongSelectMessage(Convert.ToByte(this.Parameter1.SelectedItem));
                    break;
                case MidiEvent.TuneRequest:
                    midiMessageToSend = new MidiTuneRequestMessage();
                    break;
                case MidiEvent.MidiClock:
                    midiMessageToSend = new MidiTimingClockMessage();
                    break;
                case MidiEvent.MidiStart:
                    midiMessageToSend = new MidiStartMessage();
                    break;
                case MidiEvent.MidiContinue:
                    midiMessageToSend = new MidiContinueMessage();
                    break;
                case MidiEvent.MidiStop:
                    midiMessageToSend = new MidiStopMessage();
                    break;
                case MidiEvent.ActiveSense:
                    midiMessageToSend = new MidiActiveSensingMessage();
                    break;
                case MidiEvent.Reset:
                    midiMessageToSend = new MidiSystemResetMessage();
                    break;
                default:
                    return;
            }

            // Send the message
            _synthesizer.SendMessage(midiMessageToSend);
            //this.rootPage.NotifyUser("Message sent successfully", NotifyType.StatusMessage);
        }

        private void ResetButton_Clicked(object sender, EventArgs e)
        {
            ResetMessageTypeAndParameters(true);
        }       
    }

}
