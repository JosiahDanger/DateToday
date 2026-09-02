using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Models;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class SettingsViewModel(WidgetModel widgetModel)
{
	private readonly WidgetModel _widgetModel = widgetModel;

	[RelayCommand]
	private static void CloseSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new CloseSettingsViewMessage());
	}
}
