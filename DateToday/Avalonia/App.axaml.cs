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
				An existing ServiceProvider instance cannot exist already, because
				OnFrameworkInitializationCompleted() is executed during app initialisation exactly
				once.
			""")]

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
		{
			(WidgetModel initialWidgetModel, _hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			WidgetView widgetView = new() { DataContext = new WidgetViewModel(initialWidgetModel) };

			_services = new ServiceCollection()
				.AddDomainServices(initialWidgetModel)
				.AddPresentationServices(widgetView)
				.BuildServiceProvider();

			widgetView.Opened += OnWidgetViewOpened;
			widgetView.Closing += OnWidgetViewClosing;
			widgetView.Closed += OnWidgetViewClosed;

			desktopLifetime.MainWindow = widgetView;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static bool PersistStateBeforeClosure(WidgetModel widgetModel)
	{
		JsonTypeInfo<WidgetModelDto> typeInfo =
			WidgetModelDtoSerialiserContext.Default.WidgetModelDto;

		WidgetModelDto widgetModelDto = WidgetModelDto.FromModel(widgetModel);

		try
		{
			SuspensionService.SaveState(widgetModelDto, typeInfo);
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
			if (_services == null)
			{
				throw new InvalidOperationException(
							Strings.Suspension_Exception_ServiceProviderNotInitialised);
			}

			AlertService alerts = _services.GetRequiredService<AlertService>();

			await alerts.ShowAlertAsync(
							AlertFlavour.Warning,
							Strings.Suspension_Exception_FailedToDeserialiseState_Friendly
						).ConfigureAwait(false);
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
			WidgetModel widgetModel = _services.GetRequiredService<WidgetModel>();
			bool hasSerialisationSucceeded = PersistStateBeforeClosure(widgetModel);

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
			if (sender is Window widgetView)
			{
				await Dispatcher.UIThread.InvokeAsync(() => widgetView.Close());
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
