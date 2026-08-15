using Avalonia;
using Avalonia.Media;
using System.Globalization;

namespace DateToday.Models;

internal sealed record ContentConfig(
	string DateTimeFormat,
	CultureInfo DateTimeCulture,
	byte? OrdinalDaySuffixPosition,
	uint RefreshIntervalSeconds);

internal sealed record PositionConfig(
	WindowVertexIdentifier AnchoredCorner,
	Point AnchoredCornerScaledPosition,
	string? MonitorReference,
	bool IsMouseDragEnabled);

internal sealed record FontConfig(
	FontFamily FontFamily,
	int FontSize,
	FontWeight FontWeight,
	TextRenderingMode FontRenderingMode,
	Color? CustomFontColour,
	Color? CustomDropShadowColour);

internal sealed record AppConfig(
	CultureInfo AppCulture,
	double SettingsViewOpacity
);
