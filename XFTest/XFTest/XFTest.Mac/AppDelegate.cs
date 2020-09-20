using AppKit;
using Foundation;
using Xamarin.Forms.Platform.MacOS;
using Commons.Music.Midi.macOS;
using System;

namespace XFTest.Mac
{
	[Register("AppDelegate")]
	public class AppDelegate : FormsApplicationDelegate
	{

		private NSWindow window;

		public AppDelegate()
		{
			OperatingSystem os = Environment.OSVersion;
			var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable | NSWindowStyle.Titled;

			var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
			window = new NSWindow(rect, style, NSBackingStore.Buffered, false);
			window.Title = "MyLogo 2019"; // choose your own Title here
			window.TitleVisibility = NSWindowTitleVisibility.Hidden;
			if(os.Version.Major<18)
            {
				window.Appearance = NSAppearance.GetAppearance(NSAppearance.NameVibrantDark);
            }
			else
			{
				window.Appearance = NSAppearance.GetAppearance(NSAppearance.NameDarkAqua);
			}
			NSApplication.SharedApplication.ServicesProvider = this;
		}

		public override NSWindow MainWindow { get { return window; } }

		public override void DidFinishLaunching(NSNotification notification)
		{
			global::Xamarin.Forms.Forms.Init();
			MidiSystem.Initialize(NSBundle.MainBundle.GetUrlForResource("GeneralUser GS MuseScore v1.442", "sf2"));
			LoadApplication(new App());

			base.DidFinishLaunching(notification);
		}

		public override void WillTerminate(NSNotification notification)
		{
			// Insert code here to tear down your application
		}
	}
}
