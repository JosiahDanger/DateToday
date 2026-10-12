using Avalonia;
using Avalonia.Media;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace DateToday.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal sealed class WidgetModelDto : ISuspensionState
{
	[JsonPropertyName("dateTimeFormat")]
	public required string DateTimeFormat { get; set; }

	[JsonPropertyName("dateTimeCultureIdentifier")]
	public required string DateTimeCultureIdentifier { get; set; }

	[JsonPropertyName("isOrdinalDaySuffixEnabled")]
	public bool IsOrdinalDaySuffixEnabled { get; set; }

	[JsonPropertyName("refreshIntervalSeconds")]
	public uint RefreshIntervalSeconds { get; set; }

	[JsonPropertyName("anchoredCorner")]
	public CornerIdentifier AnchoredCorner { get; set; }

	[JsonPropertyName("anchoredCornerLogicalPositionX")]
	public double AnchoredCornerLogicalPositionX { get; set; }

	[JsonPropertyName("anchoredCornerLogicalPositionY")]
	public double AnchoredCornerLogicalPositionY { get; set; }

	[JsonPropertyName("isMouseDragEnabled")]
	public required bool IsMouseDragEnabled { get; set; }

	[JsonPropertyName("fontFamilyName")]
	public required string FontFamilyName { get; set; }

	[JsonPropertyName("fontSize")]
	public int FontSize { get; set; }

	[JsonPropertyName("fontWeight")]
	public int FontWeight { get; set; }

	[JsonPropertyName("textAlignment")]
	public TextAlignment TextAlignment { get; set; }

	[JsonPropertyName("fontRenderingMode")]
	public TextRenderingMode FontRenderingMode { get; set; }

	[JsonPropertyName("fontColour")]
	public uint? FontColour { get; set; }

	[JsonPropertyName("dropShadowColour")]
	public uint? DropShadowColour { get; set; }

	[JsonPropertyName("appCultureIdentifier")]
	public required string AppCultureIdentifier { get; set; }

	[JsonPropertyName("requestedTheme")]
	public required ThemePreference RequestedTheme { get; set; }

	public WidgetModel ToModel()
	{
		CultureInfo dateTimeCulture = new(DateTimeCultureIdentifier);

		Point position = new(AnchoredCornerLogicalPositionX, AnchoredCornerLogicalPositionY);

		FontFamily fontFamily = new(FontFamilyName);

		FontWeight fontWeight = (FontWeight)FontWeight;

		Color? fontColour =
			FontColour.HasValue ?
			Color.FromUInt32(FontColour.Value) : null;

		Color? dropShadowColour =
			DropShadowColour.HasValue ?
			Color.FromUInt32(DropShadowColour.Value) : null;

		CultureInfo appCulture = new(AppCultureIdentifier);

		return new WidgetModel(

			new ContentConfig(
					DateTimeFormat,
					dateTimeCulture,
					IsOrdinalDaySuffixEnabled,
					RefreshIntervalSeconds),

			new PositionConfig(
					AnchoredCorner,
					position,
					IsMouseDragEnabled),

			new FontConfig(
					fontFamily,
					FontSize,
					fontWeight,
					TextAlignment,
					FontRenderingMode,
					fontColour,
					dropShadowColour),

			new AppConfig(
					appCulture,
					RequestedTheme));
	}

	public static WidgetModelDto FromModel(WidgetModel model)
	{
		return new WidgetModelDto
		{
			DateTimeFormat = model.Content.DateTimeFormat,
			DateTimeCultureIdentifier = model.Content.DateTimeCulture.Name,
			IsOrdinalDaySuffixEnabled = model.Content.IsOrdinalDaySuffixEnabled,
			RefreshIntervalSeconds = model.Content.RefreshIntervalSeconds,

			AnchoredCorner = model.Position.AnchoredCorner,
			AnchoredCornerLogicalPositionX = model.Position.AnchoredCornerLogicalPosition.X,
			AnchoredCornerLogicalPositionY = model.Position.AnchoredCornerLogicalPosition.Y,
			IsMouseDragEnabled = model.Position.IsMouseDragEnabled,

			FontFamilyName = model.Font.FontFamily.Name,
			FontSize = model.Font.FontSize,
			FontWeight = (int)model.Font.FontWeight,
			TextAlignment = model.Font.TextAlignment,
			FontRenderingMode = model.Font.FontRenderingMode,
			FontColour = model.Font.FontColour?.ToUInt32(),
			DropShadowColour = model.Font.DropShadowColour?.ToUInt32(),

			AppCultureIdentifier = model.App.AppCulture.Name,
			RequestedTheme = model.App.RequestedTheme
		};
	}
}
