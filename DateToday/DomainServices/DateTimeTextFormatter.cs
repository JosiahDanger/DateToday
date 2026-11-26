using DateToday.Avalonia.Resources;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DateToday.DomainServices;

internal static class DateTimeTextFormatter
{
	public static string FormatOrThrow(string format, CultureInfo culture)
	{
		try
		{
			return DateTime.Now.ToString(format, culture);
		}
		catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
		{
			throw new ValidationException(
				Strings.DateTimeTextFormatter_Exception_DateTimeFormat_Invalid, ex);
		}
	}
}
