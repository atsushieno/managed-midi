using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Commons.Music.Midi
{
	public class Mmk : Form
	{
		static readonly List<string> tone_list;

#if CHROMA_TONE
		public const bool ChromaTone = true;
#else
		public const bool ChromaTone = false;
#endif

		static Mmk ()
		{
			tone_list = new List<string> ();
			int n = 0;
			var chars = "\n".ToCharArray ();
			foreach (string s in new StreamReader (typeof (Mmk).Assembly.GetManifestResourceStream ("tonelist.txt")).ReadToEnd ().Split (chars, StringSplitOptions.RemoveEmptyEntries))
				tone_list.Add (n++ + ":" + s);
		}

		public static void Main ()
		{
			Application.Run (new Mmk ());
		}

		public Mmk ()
		{
			SetupMidiDevices ();

			this.Width = 420;
			this.Height = 300;
			this.Text = "MMK: MIDI Keyboard";

			SetupMenus ();

			var statusBar = new StatusBar ();
			Controls.Add (statusBar);

			SetupDeviceSelector ();
			SetupToneSelector ();
			SetupKeyboardLayout (KeyMap.JP106); // FIXME: make it customizible

			var rb = new RadioButton () { Text = "Normal" };
			rb.Location = new Point (30, 30);
			rb.CheckedChanged += delegate { if (rb.Checked) channel = key_channel; };
			Controls.Add (rb);

			var rb2 = new RadioButton () { Text = "Drum" };
			rb2.Location = new Point (150, 30);
			rb2.CheckedChanged += delegate { if (rb2.Checked) channel = 9; };
			Controls.Add (rb2);

			rb.Enabled = true;
		}

		void SetupMidiDevices ()
		{
			Application.ApplicationExit += delegate {
				if (output != null)
					output.Dispose ();
			};

			if (!MidiAccessManager.Default.Outputs.Any ()) {
				MessageBox.Show ("No MIDI output device was found.");
				Application.Exit ();
				return;
			}

			foreach (var dev in MidiAccessManager.Default.Outputs)
				output_devices.Add (dev);
			SwitchToDevice (0);
		}

		void SetupMenus ()
		{
			var menu = new MainMenu ();
			var file = new MenuItem ("&File");
			menu.MenuItems.Add (file);
			var exit = new MenuItem ("&Exit", delegate { QuitApplication (); }, Shortcut.CtrlQ);
			file.MenuItems.Add (exit);
			this.Menu = menu;
		}

		void SetupDeviceSelector ()
		{
			ComboBox cb = new ComboBox ();
			cb.TabIndex = 2;
			cb.Location = new Point (10, 10);
			cb.Width = 200;
			cb.DropDownStyle = ComboBoxStyle.DropDownList;
			cb.DataSource = new List<string> (from dev in output_devices select dev.Details.Name);
			cb.SelectedIndexChanged += delegate {
				try {
					this.Enabled = false;
					this.Cursor = Cursors.WaitCursor;
					if (cb.SelectedIndex < 0)
						return;
					SwitchToDevice (cb.SelectedIndex);
				} finally {
					this.Enabled = true;
					cb.Focus ();
					this.Cursor = Cursors.Default;
				}
			};
			Controls.Add (cb);
		}

		void SwitchToDevice (int deviceIndex)
		{
			if (output != null) {
				output.Dispose ();
				output = null;
			}
			output = MidiAccessManager.Default.Outputs.ElementAt (deviceIndex);
			output.SendAsync (new byte[] { (byte) (0xC0 + channel), 0, 0 }, 0, 0);

			SetupBankSelector (deviceIndex);
		}
		
		void SetupBankSelector (int deviceIndex)
		{
			var db = MidiModuleDatabase.Default.Resolve (output_devices [deviceIndex].Details.Name);
			if (db != null && db.Instrument != null && db.Instrument.Maps.Count > 0) {
				var map = db.Instrument.Maps [0];
				foreach (var prog in map.Programs) {
					var mcat = tone_menu.MenuItems [prog.Index / 8];
					var mprg = mcat.MenuItems [prog.Index % 8];
					mprg.MenuItems.Clear ();
					foreach (var bank in prog.Banks) {
						var mi = new MenuItem (String.Format ("{0}:{1} {2}", bank.Msb, bank.Lsb, bank.Name)) { Tag = bank };
						mi.Select += delegate {
							var mbank = (MidiBankDefinition) mi.Tag;
							output.SendAsync (new byte[] { (byte) (SmfEvent.CC + channel), SmfCC.BankSelect, (byte) mbank.Msb }, 0, 0);
							output.SendAsync (new byte[] { (byte) (SmfEvent.CC + channel), SmfCC.BankSelectLsb, (byte) mbank.Lsb }, 0, 0);
							output.SendAsync (new byte[] { (byte) (SmfEvent.Program + channel), (byte) mi.Parent.Tag, 0 }, 0, 0);
						};
						mprg.MenuItems.Add (mi);
					}
				}
			}
		}

		MenuItem tone_menu;
		
		static readonly string [] tone_categories = {
			"&A 0 Piano",
			"&B 8 Chromatic Percussion",
			"&C 16 Organ",
			"&D 24 Guitar",
			"&E 32 Bass",
			"&F 40 Strings",
			"&G 48 Ensemble",
			"&H 56 Brass",
			"&I 64 Reed",
			"&J 72 Pipe",
			"&K 80 Synth Lead",
			"&L 88 Synth Pad",
			"&M 96 Synth Effects",
			"&N 104 Ethnic",
			"&O 112 Percussive",
			"&P 120 SFX"
			};

		void SetupToneSelector ()
		{
			tone_menu = new MenuItem ("&Tone");
			this.Menu.MenuItems.Add (tone_menu);
			MenuItem sub = null;
			for (int i = 0; i < tone_list.Count; i++) {
				if (i % 8 == 0) {
					sub = new MenuItem (tone_categories [i / 8]);
					tone_menu.MenuItems.Add (sub);
				}
				var mi = new MenuItem (tone_list [i]);
				mi.Tag = i;
				mi.Select += delegate {
					output.SendAsync (new byte[] { (byte) (0xC0 + channel), (byte) mi.Tag, 0 }, 0, 0);
				};
				sub.MenuItems.Add (mi);
			}
		}

#if CHROMA_TONE
		static readonly string [] key_labels = {"c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b"};
#else
		static readonly string [] key_labels = {"c", "c+", "d", "d+", "e", "", "f", "f+", "g", "g+", "a", "a+", "b", ""};
#endif

		void SetupKeyboardLayout (KeyMap map)
		{
			keymap = map;

			int top = 70;

			// offset 4, 10, 18 are not mapped, so skip those numbers
			var hl = new List<Button> ();
			int labelStringIndex = key_labels.Length - 5;
			for (int i = 0; i < keymap.HighKeys.Length; i++) {
				var b = new NoteButton ();
				b.Text = key_labels [labelStringIndex % key_labels.Length];
				labelStringIndex++;
				if (!IsNotableIndex (i)) {
					b.Enabled = false;
					b.Visible = false;
				}
				b.Location = new Point (btSize / 2 + i * btSize / 2, i % 2 == 0 ? top : top + 5 + btSize);
				hl.Add (b);
				Controls.Add (b);
			}
			high_buttons = hl.ToArray ();
			var ll = new List<Button> ();
			labelStringIndex = key_labels.Length - 5;
			for (int i = 0; i < keymap.LowKeys.Length; i++) {
				var b = new NoteButton ();
				b.Text = key_labels [labelStringIndex % key_labels.Length];
				labelStringIndex++;
				if (!IsNotableIndex (i)) {
					b.Enabled = false;
					b.Visible = false;
				}
				b.Location = new Point (btSize + i * btSize / 2, i % 2 == 0 ? top + 10 + btSize * 2 : top + 15 + btSize * 3);
				ll.Add (b);
				Controls.Add (b);
			}
			low_buttons = ll.ToArray ();

			high_button_states = new bool [high_buttons.Length];
			low_button_states = new bool [low_buttons.Length];

			var tb = new TextBox ();
			tb.TabIndex = 0;
			tb.Location = new Point (10, 200);
			tb.TextChanged += delegate { tb.Text = String.Empty; };
			Controls.Add (tb);
			tb.KeyDown += delegate (object o, KeyEventArgs e) {
				ProcessKey (true, e);
			};
			tb.KeyUp += delegate (object o, KeyEventArgs e) {
				ProcessKey (false, e);
			};
		}

		static int btSize = 25;
		Button [] high_buttons;
		Button [] low_buttons;
		bool [] high_button_states;
		bool [] low_button_states;

		class NoteButton : Button
		{
			public NoteButton ()
			{
				Width = Mmk.btSize;
				Height = Mmk.btSize;
				Enabled = false;
			}

			protected override void OnGotFocus (EventArgs e)
			{
				Form.ActiveForm.Focus ();
			}
		}

		// check if the key is a notable key (in mmk).
		bool IsNotableIndex (int i)
		{
			if (ChromaTone)
				return true;

			switch (i) {
			case 4:
			case 10:
			case 18:
				return false;
			}
			return true;
		}

		void ProcessKey (bool down, KeyEventArgs e)
		{
			var key = e.KeyCode;
			switch (key) {
			case Keys.Up:
				if (octave < 7)
					octave++;
				break;
			case Keys.Down:
				if (octave > 0)
					octave--;
				break;
//			case Keys.Left:
//				transpose--;
//				break;
//			case Keys.Right:
//				transpose++;
//				break;
			default:
				var idx = keymap.LowKeys.IndexOf ((char) key);
				if (!IsNotableIndex (idx))
					return;

				if (idx >= 0)
					ProcessNodeKey (down, true, idx);
				else {
					idx = keymap.HighKeys.IndexOf ((char) key);
					if (!IsNotableIndex (idx))
						return;
					if (idx >= 0)
						ProcessNodeKey (down, false, idx);
					else
						return;
				}
				break;
			}
			e.Handled = true;
		}

		void ProcessNodeKey (bool down, bool low, int idx)
		{
			var fl = low ? low_button_states : high_button_states;
			if (fl [idx] == down)
				return; // no need to process repeated keys.

			var b = low ? low_buttons [idx] : high_buttons [idx];
			if (down)
				b.BackColor = Color.Gray;
			else
				b.BackColor = this.BackColor;
			fl [idx] = down;

			int nid = idx;
			if (!ChromaTone) {
				if (idx < 4)
					nid = idx;
				else if (idx < 10)
					nid = idx - 1;
				else if (idx < 18)
					nid = idx - 2;
				else
					nid = idx - 3;
			}

			int note;
			if (ChromaTone)
				note = octave * 12 - 4 + transpose + nid + (low ? 2 : 0);
			else
				note = (octave + (low ? 0 : 1)) * 12 - 4 + transpose + nid;

			if (0 <= note && note <= 128)
				output.SendAsync (new byte[] { (byte) ((down ? 0x90 : 0x80) + channel), (byte) note, 100 }, 0, 0);
		}

		class KeyMap
		{
			// note that those arrays do not contain non-mapped notes: index at 4, 10, 18

			// keyboard map for JP106
			// [1][2][3][4][5][6][7][8][9][0][-][^][\]
			//  [Q][W][E][R][T][Y][U][I][O][P][@][{]
			//  [A][S][D][F][G][H][J][K][L][;][:][}]
			//   [Z][X][C][V][B][N][M][<][>][?][_]
			// [UP] - octave up
			// [DOWN] - octave down
			// [LEFT] - <del>transpose decrease</del>
			// [RIGHT] - <del>transpose increase</del>

			public static readonly KeyMap JP106 = new KeyMap ("AZSXDCFVGBHNJMK\xbcL\xbe\xbb\xbf\xba\xe2\xdd", "1Q2W3E4R5T6Y7U8I9O0P\xbd\xc0\xde\xdb\xdc");

			public KeyMap (string lowKeys, string highKeys)
			{
				LowKeys = lowKeys;
				HighKeys = highKeys;
			}

			public readonly string LowKeys;
			public readonly string HighKeys;
		}

		IMidiOutput output;
		int key_channel = 1;
		int channel = 1;
		int transpose;
		int octave = 4; // lowest
		List<IMidiOutput> output_devices = new List<IMidiOutput> ();
		KeyMap keymap;

		void QuitApplication ()
		{
			// possibly show dialog in case we support MML editor buffer.
			Application.Exit ();
		}
	}
}

