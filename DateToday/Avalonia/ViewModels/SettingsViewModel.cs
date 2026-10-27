using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.DomainServices;
using DateToday.Models;
using DateToday.Utilities;
using System;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class SettingsViewModel : ObservableObject
{
	private readonly IWidgetModelSnapshot _widgetModelSnapshot;
	private readonly WidgetModelMutationService _widgetModelMutator;

	private readonly ContentConfigViewModel _contentConfigViewModel;

	public SettingsViewModel(
		IWidgetModelSnapshot widgetModelSnapshot, WidgetModelMutationService widgetModelMutator)
	{
		_widgetModelSnapshot = widgetModelSnapshot;
		_widgetModelMutator = widgetModelMutator;

		_contentConfigViewModel = new(_widgetModelSnapshot, _widgetModelMutator);
	}

	[ObservableProperty]
	public partial byte SelectedTabIndex { get; private set; }

	public ObservableObject SelectedConfigViewModel =>
		SelectedTabIndex switch
		{
			0 => _contentConfigViewModel,
			//1 => PositionConfigViewModel,
			//2 => FontConfigViewModel,
			//3 => AppConfigViewModel,
			_ => throw new ArgumentOutOfRangeException(nameof(SelectedTabIndex))
		};

	public static string AppVersion => AppVersionProvider.GetAppVersion();

	[RelayCommand]
	private static void CloseSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new CloseSettingsViewMessage());
	}
}
