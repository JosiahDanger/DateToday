﻿using Avalonia.Controls.Converters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DateToday.Converters
{
    internal sealed class ToBrushMultiConverterWithFallback : ToBrushConverter, IMultiValueConverter
    {
        public object? Convert(
            IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            /* This function will iterate through a provided list of Color objects (some other data 
             * types are also supported; see ToBrushConverter). The first object that is not null
             * will be converted into a SolidColorBrush and returned.
             * 
             * As such, the function will 'fall back' to the second list element when the first is
             * null, and so on; hence its name. */

            if (!targetType.IsAssignableFrom(typeof(IBrush)))
            {
                throw new NotSupportedException();
            }

            foreach (object? colourOrNull in values)
            {
                /* Attempt to convert via 'ToBrushConverter' the current 'values' element into a
                 * SolidColorBrush. ToBrushConverter supports arguments of various different
                 * data types. */

                object? brushOrNull = base.Convert(colourOrNull, targetType, parameter, culture);

                if (brushOrNull is IBrush brush)
                {
                    return brush;
                }
            }

            return BindingOperations.DoNothing;
        }
    }
}
