using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using DateToday.Models;
using DateToday.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Avalonia;

internal sealed partial class App : Application, IDisposable
{
	private ServiceProvider? _services;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			_services?.Dispose();

			(WidgetModel initialWidgetModel, bool hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			if (!hasDeserialisationSucceeded)
			{
				/* TODO:
				 *
				 * Display a notification to inform the user that a deserialisation error has
				 * occurred, then continue startup using the loaded default state. */
			}

			_services = new ServiceCollection().AddCommonServices(initialWidgetModel)
											   .BuildServiceProvider();

			WidgetViewModel wvm = _services.GetRequiredService<WidgetViewModel>();

			desktop.MainWindow = new WidgetView { DataContext = wvm };
			desktop.ShutdownRequested += OnShutdownRequested;
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void PersistStateBeforeShutdown()
	{
		WidgetModel? widgetModelOrNull = _services?.GetRequiredService<WidgetModel>();

		if (widgetModelOrNull is WidgetModel wm)
		{
			JsonTypeInfo<WidgetModelDto> typeInfo =
				WidgetModelDtoSerialiserContext.Default.WidgetModelDto;

			try
			{
				WidgetModelDto wmdto = WidgetModelDto.FromModel(wm);
				SuspensionService.SaveState(wmdto, typeInfo);
			}
			catch (NullReferenceException)
			{
				/* TODO:
				 *
				 * Display a notification to inform the user that the WidgetModel is corrupt, then
				 * terminate the application. */
			}
			catch (InvalidOperationException)
			{
				/* TODO:
				 *
				 * Display a notification to inform the user that a serialisation error has
				 * occurred, then terminate the application. */
			}
		}
	}

	private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
	{
		PersistStateBeforeShutdown();
		Dispose();
	}

	public void Dispose()
	{
		_services?.Dispose();
	}
}
