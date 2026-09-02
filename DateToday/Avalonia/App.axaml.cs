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
using System;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Avalonia;

internal sealed partial class App : Application
{
	private bool _isAppShutdownAfoot;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
		{
			(WidgetModel? widgetModel, bool hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			WidgetView widgetView = new() { DataContext = new WidgetViewModel(widgetModel) };
			WidgetDialogService widgetDialogService = new(widgetView, widgetModel);

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
}
