using Avalonia.Controls;
using Avalonia.Data.Converters;
using DateToday.Avalonia.Resources;
using System;

namespace DateToday.Avalonia.Converters;

internal static class SettingsNavigatorConverters
{
	public static readonly IValueConverter SettingsNavigatorIndexToViewConverter =
		new FuncValueConverter<int, Control>(index =>
			index switch
			{
				//0 => new ContentConfigView(),
				//1 => new PositionConfigView(),
				//2 => new FontConfigView(),
				//3 => new AppConfigView(),
				_ => throw new NotSupportedException(
								Strings.SettingsViewConverters_Exception_SettingsNavigatorIndexNotSupported)
			});
}
