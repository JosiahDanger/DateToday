using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.DomainServices;
using DateToday.Models;
using System;
using System.Threading.Tasks;

namespace DateToday.Avalonia.PresentationServices;

internal sealed class WidgetDialogService : IDisposable
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
		if (!_widgetView.IsVisible)
		{
			return;
		}

		SettingsViewModel settingsViewModel = new(_widgetModelMutationService);
		SettingsView settingsView = new() { DataContext = settingsViewModel };

		await settingsView.ShowDialog(_widgetView).ConfigureAwait(false);
	}

	public async Task ShowAlertAsync(AlertFlavour flavour, string message)
	{
		if (!_widgetView.IsVisible)
		{
			return;
		}

		AlertModel alertModel = new(flavour, message);
		AlertViewModel alertViewModel = new(alertModel);
		AlertView alertView = new() { DataContext = alertViewModel };

		await alertView.ShowDialog(_widgetView).ConfigureAwait(false);
	}

	public void Dispose()
	{
		WeakReferenceMessenger.Default.Unregister<OpenSettingsViewMessage>(this);
	}
}
