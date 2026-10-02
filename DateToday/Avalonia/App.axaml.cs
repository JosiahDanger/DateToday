using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DateToday.Avalonia.PresentationServices;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.DependencyInjection;
using DateToday.DomainServices;
using DateToday.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Avalonia;

internal sealed partial class App : Application, IDisposable
{
	private ServiceProvider? _services;
	private bool _isAppShutdownAfoot;

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
			(WidgetModel? widgetModel, bool hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			_services = new ServiceCollection()
				.AddDomainServices(widgetModel)
				.AddPresentationServices(this)
				.BuildServiceProvider();

			WidgetView widgetView = _services.GetRequiredService<WidgetView>();

			WidgetViewModel widgetViewModel = _services.GetRequiredService<WidgetViewModel>();

			WidgetDialogService widgetDialogService =
				_services.GetRequiredService<WidgetDialogService>();

			widgetView.DataContext = widgetViewModel;

			async void OnWidgetViewOpened(object? sender, EventArgs e)
			{
				if (!hasDeserialisationSucceeded)
				{
					await widgetDialogService.ShowAlertAsync(
						AlertFlavour.Warning,
						Strings.Suspension_Exception_FailedToDeserialiseState_Friendly
					).ConfigureAwait(false);
				}
			}

			async void OnWidgetViewClosing(object? sender, WindowClosingEventArgs e)
			{
				if (_isAppShutdownAfoot)
				{
					return;
				}

				e.Cancel = true;
				_isAppShutdownAfoot = true;

				try
				{
					bool hasSerialisationSucceeded = PersistStateBeforeClosure(widgetModel);

					if (!hasSerialisationSucceeded)
					{
						await widgetDialogService.ShowAlertAsync(
							AlertFlavour.Warning,
							Strings.Suspension_Exception_FailedToPersistState_Friendly
						).ConfigureAwait(false);
					}
				}
				finally
				{
					await Dispatcher.UIThread.InvokeAsync(() => widgetView.Close());
					this.Dispose();
				}
			}

			widgetView.Opened += OnWidgetViewOpened;
			widgetView.Closing += OnWidgetViewClosing;

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

	public void Dispose()
	{
		_services?.Dispose();
	}
}
