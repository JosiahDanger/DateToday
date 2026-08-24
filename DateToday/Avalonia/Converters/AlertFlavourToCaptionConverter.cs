using Avalonia.Data.Converters;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.Globalization;

namespace DateToday.Avalonia.Converters
{
    internal class AlertFlavourToCaptionConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
			if (value is AlertFlavour inputAlertFlavour)
			{
				switch (inputAlertFlavour)
				{
					case AlertFlavour.Information:
						return Strings.AlertView_InformationFlavour_Caption;

					case AlertFlavour.Warning:
						return Strings.AlertView_WarningFlavour_Caption;

					case AlertFlavour.Error:
						return Strings.AlertView_ErrorFlavour_Caption;
				}
			}

			throw new NotSupportedException();
		}

		public object ConvertBack(
			object? value, Type targetType, object? parameter, CultureInfo culture)
        {
			throw new NotSupportedException();
		}
	}
}
