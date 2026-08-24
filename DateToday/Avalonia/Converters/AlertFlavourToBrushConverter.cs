using Avalonia.Data.Converters;
using Avalonia.Media;
using DateToday.Models;
using System;
using System.Globalization;

namespace DateToday.Avalonia.Converters
{
    internal class AlertFlavourToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
			if (value is AlertFlavour inputAlertFlavour)
			{
				switch (inputAlertFlavour)
				{
					case AlertFlavour.Information:
						return new SolidColorBrush(Colors.SteelBlue);

					case AlertFlavour.Warning:
						return new SolidColorBrush(Colors.Goldenrod);

					case AlertFlavour.Error:
						return new SolidColorBrush(Colors.IndianRed);
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
