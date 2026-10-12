using Avalonia;
using Avalonia.Media;
using System.Globalization;

namespace DateToday.Models;

internal sealed record ContentConfig(
	string DateTimeFormat,
	CultureInfo DateTimeCulture,
	bool IsOrdinalDaySuffixEnabled,
	uint RefreshIntervalSeconds);

internal sealed record PositionConfig(
	CornerIdentifier AnchoredCorner,
	Point AnchoredCornerLogicalPosition,
	bool IsMouseDragEnabled);

internal sealed record FontConfig(
	FontFamily FontFamily,
	int FontSize,
	FontWeight FontWeight,
	TextAlignment TextAlignment,
	TextRenderingMode FontRenderingMode,
	Color? FontColour,
	Color? DropShadowColour);

internal sealed record AppConfig(
	CultureInfo AppCulture,
	ThemePreference RequestedTheme);
