using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.DomainServices;
using DateToday.Models;
using DateToday.Utilities;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class SettingsViewModel(
	IWidgetModelSnapshot widgetModelSnapshot, WidgetModelMutationService widgetModelMutator)
{
	private readonly IWidgetModelSnapshot _widgetModelSnapshot = widgetModelSnapshot;
	private readonly WidgetModelMutationService _widgetModelMutator = widgetModelMutator;

	public static string AppVersion => AppVersionProvider.GetAppVersion();

	[RelayCommand]
	private static void CloseSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new CloseSettingsViewMessage());
	}
}
