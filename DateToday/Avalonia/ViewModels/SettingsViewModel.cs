using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Services;
using DateToday.Utilities;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class SettingsViewModel(WidgetModelMutationService widgetModelMutator)
{
	private readonly WidgetModelMutationService _widgetModelMutator = widgetModelMutator;

	public static string AppVersion => AppVersionProvider.GetAppVersion();

	[RelayCommand]
	private static void CloseSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new CloseSettingsViewMessage());
	}
}
