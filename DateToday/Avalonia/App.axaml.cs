using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.Models;
using DateToday.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace DateToday.Avalonia;

internal sealed partial class App : Application, IDisposable
{
	private ServiceProvider? _services;
	private IClassicDesktopStyleApplicationLifetime? _desktopLifetime;
	private WidgetView? _widgetView;
	private bool _hasDeserialisationSucceeded;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
		{
			_services?.Dispose();

			_desktopLifetime = desktopLifetime;

			(WidgetModel initialWidgetModel, _hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			_widgetView = new() { DataContext = new WidgetViewModel(initialWidgetModel) };

			_services = new ServiceCollection()
				.AddCommonServices(initialWidgetModel)
				.AddTransient<AlertService>(_ => new AlertService(_widgetView))
				.BuildServiceProvider();

			_widgetView.Opened += OnWidgetViewOpened;
			_widgetView.Closing += OnWidgetViewClosing;
			_widgetView.Closed += OnWidgetViewClosed;

			_desktopLifetime.MainWindow = _widgetView;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static async Task<bool> PersistStateBeforeClosure(WidgetModel widget)
	{
		JsonTypeInfo<WidgetModelDto> typeInfo =
			WidgetModelDtoSerialiserContext.Default.WidgetModelDto;

		WidgetModelDto wmdto = WidgetModelDto.FromModel(widget);

		try
		{
			SuspensionService.SaveState(wmdto, typeInfo);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		return true;
	}

	private async void OnWidgetViewOpened(object? sender, EventArgs e)
	{
		if (!_hasDeserialisationSucceeded)
		{
			AlertService? alerts = _services?.GetRequiredService<AlertService>();

			if (alerts != null)
			{
				await alerts.ShowAlertAsync(
								AlertFlavour.Warning,
								Strings.Suspension_Exception_FailedToDeserialiseState_Friendly
							).ConfigureAwait(false);
			}
		}
	}

	private async void OnWidgetViewClosing(object? sender, WindowClosingEventArgs e)
	{
		if (e.CloseReason == WindowCloseReason.ApplicationShutdown)
		{
			return;
		}

		if (_services != null)
		{
			WidgetModel widget = _services.GetRequiredService<WidgetModel>();

			bool hasSerialisationSucceeded =
				await PersistStateBeforeClosure(widget).ConfigureAwait(false);

			if (!hasSerialisationSucceeded)
			{
				AlertService alerts = _services.GetRequiredService<AlertService>();

				e.Cancel = true;

				await alerts.ShowAlertAsync(
								AlertFlavour.Warning,
								Strings.Suspension_Exception_FailedToPersistState_Friendly
							).ConfigureAwait(false);

				_desktopLifetime?.Shutdown();
			}
		}
	}

	private void OnWidgetViewClosed(object? sender, EventArgs e)
	{
		Dispose();
	}

	public void Dispose()
	{
		_services?.Dispose();
	}
}
