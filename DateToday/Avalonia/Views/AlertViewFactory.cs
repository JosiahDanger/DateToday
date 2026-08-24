using DateToday.Avalonia.ViewModels;
using DateToday.Models;

namespace DateToday.Avalonia.Views;

internal static class AlertViewFactory
{
	internal static AlertView CreateAlertView(AlertFlavour flavour, string message)
	{
		AlertModel model = new(flavour, message);
		AlertViewModel viewModel = new(model);

		return new AlertView { DataContext = viewModel };
	}
}
