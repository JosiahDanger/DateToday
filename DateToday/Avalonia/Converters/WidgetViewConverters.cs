using Avalonia.Data.Converters;
using DateToday.Avalonia.Resources;

namespace DateToday.Avalonia.Converters;

internal static class WidgetViewConverters
{
	public static readonly IValueConverter IsMouseDragEnabledToggleLabelConverter =
		new FuncValueConverter<bool, string>(isMouseDragEnabled =>
			isMouseDragEnabled
				? Strings.WidgetView_ContextMenu_MenuItem_DisableMouseDrag
				: Strings.WidgetView_ContextMenu_MenuItem_EnableMouseDrag);
}
