using DateToday.Avalonia.ViewModels;
using DateToday.DomainServices;
using DateToday.Models;

namespace DateToday.Avalonia.Views;

internal static class SettingsViewFactory
{
	public static SettingsView CreateSettingsView(
		IWidgetModelSnapshot widgetModelSnapshot,
		WidgetModelMutationService widgetModelMutationService)
	{
		SettingsViewModel settingsViewModel = new(widgetModelSnapshot, widgetModelMutationService);
		SettingsView settingsView = new() { DataContext = settingsViewModel };

		return settingsView;
	}
}
