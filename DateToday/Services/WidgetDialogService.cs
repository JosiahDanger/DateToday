using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.Models;
using System.Threading.Tasks;

namespace DateToday.Services;

internal sealed class WidgetDialogService
{
	private readonly WidgetView _widgetView;
	private readonly WidgetModelMutationService _widgetModelMutationService;

	public WidgetDialogService(WidgetView widgetView, WidgetModelMutationService widgetModelMutator)
	{
		_widgetView = widgetView;
		_widgetModelMutationService = widgetModelMutator;

		WeakReferenceMessenger.Default.Register<OpenSettingsViewMessage>(
			this, async (_, _) => await this.ShowSettingsAsync().ConfigureAwait(false));
	}

	public async Task ShowSettingsAsync()
	{
		SettingsViewModel settingsViewModel = new(_widgetModelMutationService);
		SettingsView settingsView = new() { DataContext = settingsViewModel };

		await settingsView.ShowDialog(_widgetView).ConfigureAwait(false);
	}

	public async Task ShowAlertAsync(AlertFlavour flavour, string message)
	{
		AlertModel alertModel = new(flavour, message);
		AlertViewModel alertViewModel = new(alertModel);
		AlertView alertView = new() { DataContext = alertViewModel };

		await alertView.ShowDialog(_widgetView).ConfigureAwait(false);
	}
}
