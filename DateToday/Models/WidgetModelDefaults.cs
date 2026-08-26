using Avalonia.Media;
using System.Globalization;

namespace DateToday.Models;

internal static class WidgetModelDefaults
{
	public static ContentConfig DefaultContentConfig =>
		new(
			DateTimeFormat: "dddd, \"the\" d \"of\" MMMM",
			DateTimeCulture: CultureInfo.CurrentCulture,
			OrdinalDaySuffixPosition: 13,
			RefreshIntervalSeconds: 60);

	public static PositionConfig DefaultPositionConfig =>
		new(
			AnchoredCorner: WindowVertexIdentifier.TopRight,
			AnchoredCornerScaledPosition: new(100, 100),
			MonitorReference: null,
			IsMouseDragEnabled: false);

	public static FontConfig DefaultFontConfig =>
		new(
			FontFamily: FontFamily.Default,
			FontSize: 70,
			FontWeight: FontWeight.Normal,
			FontRenderingMode: TextRenderingMode.SubpixelAntialias,
			CustomFontColour: null,
			CustomDropShadowColour: null);

	public static AppConfig DefaultAppConfig =>
		new(
			AppCulture: CultureInfo.CurrentUICulture);
}
