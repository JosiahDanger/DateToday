using Avalonia.Data.Converters;
using Avalonia.Media;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;

namespace DateToday.Avalonia.Converters;

internal static class AlertFlavourConverters
{
	public static readonly IValueConverter AlertFlavourToCaptionConverter =
		new FuncValueConverter<AlertFlavour, string>(flavour =>
			flavour switch
			{
				AlertFlavour.Information => Strings.AlertView_InformationFlavour_Caption,
				AlertFlavour.Warning => Strings.AlertView_WarningFlavour_Caption,
				AlertFlavour.Error => Strings.AlertView_ErrorFlavour_Caption,
				_ => throw new NotSupportedException(
									Strings.Converter_Exception_EnumerationTypeMember_NotSupported),
			});

	public static readonly IValueConverter AlertFlavourToBrushConverter =
		new FuncValueConverter<AlertFlavour, SolidColorBrush>(flavour =>
			flavour switch
			{
				AlertFlavour.Information => new SolidColorBrush(Colors.SteelBlue),
				AlertFlavour.Warning => new SolidColorBrush(Colors.Goldenrod),
				AlertFlavour.Error => new SolidColorBrush(Colors.IndianRed),
				_ => throw new NotSupportedException(
									Strings.Converter_Exception_EnumerationTypeMember_NotSupported),
			});
}
