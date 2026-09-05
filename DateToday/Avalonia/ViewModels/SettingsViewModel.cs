using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Models;
using DateToday.Utilities;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class SettingsViewModel(WidgetModel widgetModel)
{
	private readonly WidgetModel _widgetModel = widgetModel;

	public static string AppVersion => AppVersionProvider.GetAppVersion();

	[RelayCommand]
	private static void CloseSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new CloseSettingsViewMessage());
	}
}
