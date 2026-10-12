using Avalonia.Media;
using System.Globalization;

namespace DateToday.Models;

internal static class WidgetModelDefaults
{
	public static ContentConfig DefaultContentConfig =>
		new(
			DateTimeFormat: "dddd, \"the\" d \"of\" MMMM",
			DateTimeCulture: CultureInfo.CurrentCulture,
			IsOrdinalDaySuffixEnabled: false,
			RefreshIntervalSeconds: 60);

	public static PositionConfig DefaultPositionConfig =>
		new(
			AnchoredCorner: CornerIdentifier.TopRight,
			AnchoredCornerLogicalPosition: new(100, 100),
			IsMouseDragEnabled: true);

	public static FontConfig DefaultFontConfig =>
		new(
			FontFamily: FontFamily.Default,
			FontSize: 70,
			FontWeight: FontWeight.Normal,
			TextAlignment: TextAlignment.Center,
			FontRenderingMode: TextRenderingMode.Unspecified,
			FontColour: null,
			DropShadowColour: null);

	public static AppConfig DefaultAppConfig =>
		new(
			AppCulture: CultureInfo.CurrentUICulture,
			RequestedTheme: ThemePreference.Automatic);
}
