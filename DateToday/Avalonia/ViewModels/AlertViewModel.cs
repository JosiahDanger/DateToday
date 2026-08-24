using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Models;

namespace DateToday.Avalonia.ViewModels;

internal sealed partial class AlertViewModel(AlertModel alert)
{
	public AlertModel Alert { get; } = alert;

	[RelayCommand]
	private static void CloseAlert()
	{
		WeakReferenceMessenger.Default.Send(new CloseAlertMessage());
	}
}
