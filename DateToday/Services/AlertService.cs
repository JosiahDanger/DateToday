using Avalonia.Controls;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace DateToday.Services;

[SuppressMessage(
	"Microsoft.Performance",
	"CA1812:AvoidUninstantiatedInternalClasses",
	Justification =
		"""
			AlertService is registered as a transient service via
			ServiceCollectionExtensions.AddPresentationServices() and is instantiated by means of
			dependency injection.
		""")]

internal sealed class AlertService(Lazy<Window> parentWindow) : IAlertService
{
	private readonly Lazy<Window> _parentWindow = parentWindow;

	public async Task ShowAlertAsync(AlertFlavour flavour, string message)
	{
		AlertModel alertModel = new(flavour, message);
		AlertViewModel alertViewModel = new(alertModel);
		AlertView alertView = new() { DataContext = alertViewModel };

		await alertView.ShowDialog(_parentWindow.Value).ConfigureAwait(false);
	}
}
