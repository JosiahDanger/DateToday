using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Models;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class AlertViewModel(AlertModel alertModel)
{
	public AlertModel AlertModel { get; } = alertModel;

	[RelayCommand]
	private static void CloseAlertView()
	{
		WeakReferenceMessenger.Default.Send(new CloseAlertViewMessage());
	}
}
