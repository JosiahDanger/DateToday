using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
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

	public WidgetDialogService(
		WidgetView widgetView, WidgetModelMutationService widgetModelMutationService)
	{
		_widgetView = widgetView;
		_widgetModelMutationService = widgetModelMutationService;

		WeakReferenceMessenger.Default.Register<OpenSettingsViewMessage>(
			this, async (_, _) => await this.ShowSettingsAsync().ConfigureAwait(false));
	}

	public async Task ShowSettingsAsync()
	{
		if (!_widgetView.IsVisible)
		{
			return;
		}

		SettingsView settingsView =
			SettingsViewFactory.CreateSettingsView(_widgetModelMutationService);

		await settingsView.ShowDialog(_widgetView).ConfigureAwait(false);
	}

	public async Task ShowAlertAsync(AlertFlavour flavour, string message)
	{
		if (!_widgetView.IsVisible)
		{
			return;
		}

		AlertView alertView = AlertViewFactory.CreateAlertView(flavour, message);

		await alertView.ShowDialog(_widgetView).ConfigureAwait(false);
	}

	public void Dispose()
	{
		WeakReferenceMessenger.Default.Unregister<OpenSettingsViewMessage>(this);
	}
}
