using Avalonia.Controls;
using DateToday.Avalonia.Views;
using DateToday.Models;
using System;
using System.Threading.Tasks;

namespace DateToday.Services;

internal sealed class AlertService(Lazy<Window> parentWindow) : IAlertService
{
	private readonly Lazy<Window> _parentWindow = parentWindow;

	public async Task ShowAlertAsync(AlertFlavour flavour, string message)
	{
		AlertView alert = AlertViewFactory.CreateAlertView(flavour, message);

		await alert.ShowDialog(_parentWindow.Value).ConfigureAwait(false);
	}
}
