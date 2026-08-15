using Avalonia;
using DateToday.Avalonia.Media;
using System;

namespace DateToday.Avalonia;

internal sealed class Program
{
	private static readonly FontCollection _embeddedFonts = new();

	/* Initialization code. Don't use any Avalonia, third-party APIs or any
	 * SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet
	 * and stuff might break. */

	[STAThread]
	public static void Main(string[] args)
		=> BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
					 .UsePlatformDetect()
					 .ConfigureFonts(fontManager => fontManager.AddFontCollection(_embeddedFonts))
					 .LogToTrace();
}
