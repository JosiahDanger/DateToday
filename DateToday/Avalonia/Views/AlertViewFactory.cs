using DateToday.Avalonia.ViewModels;
using DateToday.Models;

namespace DateToday.Avalonia.Views;

internal static class AlertViewFactory
{
	public static AlertView CreateAlertView(AlertFlavour alertFlavour, string message)
	{
		AlertModel alertModel = new(alertFlavour, message);
		AlertViewModel alertViewModel = new(alertModel);
		AlertView alertView = new() { DataContext = alertViewModel };

		return alertView;
	}
}
