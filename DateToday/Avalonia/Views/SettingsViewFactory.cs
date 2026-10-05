using DateToday.Avalonia.ViewModels;
using DateToday.DomainServices;

namespace DateToday.Avalonia.Views;

internal static class SettingsViewFactory
{
	public static SettingsView CreateSettingsView(
		WidgetModelMutationService widgetModelMutationService)
	{
		SettingsViewModel settingsViewModel = new(widgetModelMutationService);
		SettingsView settingsView = new() { DataContext = settingsViewModel };

		return settingsView;
	}
}
