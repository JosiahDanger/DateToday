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

	[JsonPropertyName("ordinalDaySuffixPosition")]
	public byte? OrdinalDaySuffixPosition { get; set; }

	[JsonPropertyName("refreshIntervalSeconds")]
	public uint RefreshIntervalSeconds { get; set; }

	[JsonPropertyName("anchoredCorner")]
	public WindowVertexIdentifier AnchoredCorner { get; set; }

	[JsonPropertyName("anchoredCornerScaledPositionX")]
	public double AnchoredCornerScaledPositionX { get; set; }

	[JsonPropertyName("anchoredCornerScaledPositionY")]
	public double AnchoredCornerScaledPositionY { get; set; }

	[JsonPropertyName("monitorReference")]
	public string? MonitorReference { get; set; }

	[JsonPropertyName("isMouseDragEnabled")]
	public required bool IsMouseDragEnabled { get; set; }

	[JsonPropertyName("fontFamilyName")]
	public required string FontFamilyName { get; set; }

	[JsonPropertyName("fontSize")]
	public int FontSize { get; set; }

	[JsonPropertyName("fontWeight")]
	public int FontWeight { get; set; }

	[JsonPropertyName("fontRenderingMode")]
	public TextRenderingMode FontRenderingMode { get; set; }

	[JsonPropertyName("customFontColour")]
	public uint? CustomFontColour { get; set; }

	[JsonPropertyName("customDropShadowColour")]
	public uint? CustomDropShadowColour { get; set; }

	[JsonPropertyName("appCultureIdentifier")]
	public required string AppCultureIdentifier { get; set; }

	public WidgetModel ToModel()
	{
		CultureInfo dateTimeCulture = new(DateTimeCultureIdentifier);

		Point position = new(AnchoredCornerScaledPositionX, AnchoredCornerScaledPositionY);

		FontFamily fontFamily = new(FontFamilyName);

		FontWeight fontWeight = (FontWeight)FontWeight;

		Color? customFontColour =
			CustomFontColour.HasValue ?
			Color.FromUInt32(CustomFontColour.Value) : null;

		Color? customDropShadowColour =
			CustomDropShadowColour.HasValue ?
			Color.FromUInt32(CustomDropShadowColour.Value) : null;

		CultureInfo appCulture = new(AppCultureIdentifier);

		return new WidgetModel(

			new ContentConfig(
					DateTimeFormat,
					dateTimeCulture,
					OrdinalDaySuffixPosition,
					RefreshIntervalSeconds),

			new PositionConfig(
					AnchoredCorner,
					position,
					MonitorReference,
					IsMouseDragEnabled),

			new FontConfig(
					fontFamily,
					FontSize,
					fontWeight,
					FontRenderingMode,
					customFontColour,
					customDropShadowColour),

			new AppConfig(
					appCulture));
	}

	public static WidgetModelDto FromModel(WidgetModel model)
	{
		return new WidgetModelDto
		{
			DateTimeFormat = model.Content.DateTimeFormat,
			DateTimeCultureIdentifier = model.Content.DateTimeCulture.Name,
			OrdinalDaySuffixPosition = model.Content.OrdinalDaySuffixPosition,
			RefreshIntervalSeconds = model.Content.RefreshIntervalSeconds,

			AnchoredCorner = model.Position.AnchoredCorner,
			AnchoredCornerScaledPositionX = model.Position.AnchoredCornerScaledPosition.X,
			AnchoredCornerScaledPositionY = model.Position.AnchoredCornerScaledPosition.Y,
			MonitorReference = model.Position.MonitorReference,
			IsMouseDragEnabled = model.Position.IsMouseDragEnabled,

			FontFamilyName = model.Font.FontFamily.Name,
			FontSize = model.Font.FontSize,
			FontWeight = (int)model.Font.FontWeight,
			FontRenderingMode = model.Font.FontRenderingMode,
			CustomFontColour = model.Font.CustomFontColour?.ToUInt32(),
			CustomDropShadowColour = model.Font.CustomDropShadowColour?.ToUInt32(),

			AppCultureIdentifier = model.App.AppCulture.Name
		};
	}
}
