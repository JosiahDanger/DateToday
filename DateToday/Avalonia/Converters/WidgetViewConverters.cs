using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DateToday.Avalonia.Resources;
using System;
using System.Collections.Generic;

namespace DateToday.Avalonia.Converters;

internal static class WidgetViewConverters
{
	public static readonly IValueConverter ToggleMouseDragLabelConverter =
		new FuncValueConverter<bool, string>(isMouseDragEnabled =>
			isMouseDragEnabled
				? Strings.WidgetView_ContextMenu_MenuItem_DisableMouseDrag
				: Strings.WidgetView_ContextMenu_MenuItem_EnableMouseDrag);

	public static readonly IValueConverter ToggleMouseDragStreamGeometryConverter =
		new FuncValueConverter<bool, StreamGeometry>(isMouseDragEnabled =>
		{
			string resourceKey = isMouseDragEnabled ? "lock_regular" : "unlock_regular";

			if (Application.Current != null)
			{
				object? resource = Application.Current.FindResource(resourceKey);

				if (resource is StreamGeometry icon)
				{
					return icon;
				}

				throw new KeyNotFoundException(
					Strings.WidgetViewConverters_Exception_GeometryResource_Null);
			}

			throw new InvalidOperationException(
				Strings.WidgetViewConverters_Exception_ApplicationCurrent_Null);
		});
}
