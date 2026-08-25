using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.Models;
using DateToday.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Avalonia;

internal sealed partial class App : Application, IDisposable
{
	private ServiceProvider? _services;
	private WidgetView? _widgetView;
	private bool _hasDeserialisationSucceeded, _isAppShutdownAfoot;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	[SuppressMessage(
		"IDisposableAnalyzers.IDISP003",
		"IDISP003",
		Justification =
			"""
				An existing ServiceProvider instance cannot exist, because
				OnFrameworkInitializationCompleted() is called only once during app initialisation
				by the Avalonia framework.
			""")]

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
		{
			(WidgetModel initialWidgetModel, _hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			_widgetView = new() { DataContext = new WidgetViewModel(initialWidgetModel) };

			_services = new ServiceCollection()
				.AddDomainServices(initialWidgetModel)
				.AddPresentationServices(_widgetView)
				.BuildServiceProvider();

			_widgetView.Opened += OnWidgetViewOpened;
			_widgetView.Closing += OnWidgetViewClosing;
			_widgetView.Closed += OnWidgetViewClosed;

			desktopLifetime.MainWindow = _widgetView;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static bool PersistStateBeforeClosure(WidgetModel widget)
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
		if (_isAppShutdownAfoot)
		{
			return;
		}

		if (_services == null)
		{
			throw new InvalidOperationException(
						Strings.Suspension_Exception_ServiceProviderNotInitialised);
		}

		e.Cancel = true;
		_isAppShutdownAfoot = true;

		try
		{
			WidgetModel widget = _services.GetRequiredService<WidgetModel>();
			bool hasSerialisationSucceeded = PersistStateBeforeClosure(widget);

			if (!hasSerialisationSucceeded)
			{
				AlertService alerts = _services.GetRequiredService<AlertService>();

				await alerts.ShowAlertAsync(
					AlertFlavour.Warning,
					Strings.Suspension_Exception_FailedToPersistState_Friendly
				).ConfigureAwait(false);
			}
		}
		finally
		{
			await Dispatcher.UIThread.InvokeAsync(() => _widgetView?.Close());
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
