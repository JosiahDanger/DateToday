using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DateToday.Converters
{
    internal class EnumToBooleanConverter : IValueConverter
    {
        public object? Convert(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum inputEnum && parameter is Enum targetEnum)
            {
                return inputEnum.Equals(targetEnum);
            }

            throw new NotSupportedException();
        }

        public object? ConvertBack(
            object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isTargetEnumSelected && parameter is Enum targetEnum)
            {
                if (isTargetEnumSelected)
                {
                    return targetEnum;
                }

                return BindingOperations.DoNothing;
            }

            throw new NotSupportedException();
        }
    }
}
