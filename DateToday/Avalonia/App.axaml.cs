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
using System.Threading.Tasks;

namespace DateToday.Avalonia;

internal sealed partial class App : Application, IDisposable
{
	/// <summary>
	/// The dependency injection container. Instantiated exactly once in
	/// <see cref="OnFrameworkInitializationCompleted"/>. It remains active throughout the entire
	/// application lifetime.
	/// </summary>

	private ServiceProvider? _services;
	private bool _hasDeserialisationSucceeded;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	/// <summary>
	/// Executed immediately after the Avalonia framework is initialised. It is responsible for
	/// instantiating the application dependency injection container, deserialising the persisted
	/// application state should it exist, and subscribing to lifetime event handlers of the primary
	/// application window (<see cref="WidgetView" />).
	/// </summary>
	/// <remarks>
	/// In the event that a persisted application state exists, deserialisation success is recorded
	/// in <see cref="_hasDeserialisationSucceeded" /> for later error handling. Note that, if no
	/// persisted state exists, this flag will always reflect a Success state.
	/// </remarks>

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
			(WidgetModel? widgetModel, _hasDeserialisationSucceeded) =
				WidgetModelFactory.GetInitialWidgetModel();

			_services = new ServiceCollection()
				.AddDomainServices(widgetModel)
				.AddPresentationServices(this)
				.BuildServiceProvider();

			WidgetView widgetView = _services.GetRequiredService<WidgetView>();
			WidgetViewModel widgetViewModel = _services.GetRequiredService<WidgetViewModel>();

			widgetView.DataContext = widgetViewModel;

			widgetView.Opened += OnWidgetViewOpened;
			widgetView.Closing += OnWidgetViewClosing;

			desktopLifetime.MainWindow = widgetView;
		}

		base.OnFrameworkInitializationCompleted();
	}

	/// <summary>
	/// Attempts to serialise the <paramref name="widgetModel"/> to persistent storage.
	/// </summary>
	/// <returns>
	/// <c>true</c> if serialisation succeeded; <c>false</c> if serialisation failed.
	/// </returns>
	/// <remarks>
	/// In order to allow the application shutdown process to continue gracefully in the event of a
	/// serialisation failure, exceptions are caught internally; Error state is reflected in the
	/// returned boolean value.
	/// </remarks>

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

	/// <summary>
	///	CleanUpAndCloseAsync(…) orchestrates the app shutdown sequence, including event
	///	unsubscription. It attempts to serialise the singleton <see cref="WidgetModel" /> state, and
	///	if it fails, notifies the user via an alert dialog.
	/// </summary>
	/// <remarks>
	/// This method is executed asynchronously because it must await
	/// <see cref="WidgetDialogService.ShowAlertAsync"/> and <see cref="Dispatcher.InvokeAsync"/>.
	/// The alert dialog must be closed via the UI thread before its parent window closes, otherwise
	/// a runtime error will occur. To ensure the intended chronology of events, closure of the
	/// <see cref="WidgetView" /> is marshalled back to the UI thread via the Dispatcher. Thus is
	/// satisfied Avalonia's requirement that windows are closed on the UI thread. Finally, note
	/// that the method's unsubscription from <see cref="Window.Closing" /> is absolutely necessary
	/// in order to prevent unintended UI behaviour in the event that the WidgetModel state could
	/// not be persisted.
	/// </remarks>

	private async Task CleanUpAndCloseAsync(
		WidgetModel widgetModel, WidgetView widgetView, WidgetDialogService widgetDialogService)
	{
		widgetView.Opened -= OnWidgetViewOpened;
		widgetView.Closing -= OnWidgetViewClosing;

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

	/// <summary>
	/// Handles the <see cref="WidgetView" /> <see cref="Window.Closing"/> event. Immediately
	/// prevents window closure and initiates asynchronous cleanup of the application state and
	/// services.
	/// </summary>
	/// <remarks>
	/// Closure of WidgetView is cancelled immediately in order to allow
	/// <see cref="CleanUpAndCloseAsync"/> to carry out its cleanup and shutdown operations before
	/// the application is terminated. This independent asynchronous cleanup task is intentionally
	/// not awaited; application shutdown will proceed afterwards.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the dependency injection container has not been instantiated, or if the event
	/// sender is not the expected WidgetView instance. Note that, even if application shutdown is
	/// instigated by the operating system, such as through the Task Manager, the event sender has
	/// been observed to be the WidgetView.
	/// </exception>

	private void OnWidgetViewClosing(object? sender, WindowClosingEventArgs e)
	{
		if (_services == null)
		{
			throw new InvalidOperationException(Strings.Application_Exception_ServiceProvider_Null);
		}

		if (sender is not WidgetView widgetView)
		{
			throw new InvalidOperationException(
				Strings.Application_Exception_UnsupportedWidgetViewClosureInstigator);
		}

		e.Cancel = true;

		WidgetModel widgetModel = _services.GetRequiredService<WidgetModel>();
		WidgetDialogService widgetDialogService =
			_services.GetRequiredService<WidgetDialogService>();

		_ = CleanUpAndCloseAsync(widgetModel, widgetView, widgetDialogService);
	}

	/// <summary>
	/// Handles the <see cref="WidgetView" /> <see cref="Window.Opened"/> event. An alert is
	/// displayed to the user if prior deserialisation of the application state by
	/// <see cref="OnFrameworkInitializationCompleted" /> was unsuccessful.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the <see cref="WidgetDialogService"/> cannot be resolved. This would indicate a
	/// dependency injection configuration error and should not occur during normal operation.
	/// </exception>

	private async void OnWidgetViewOpened(object? sender, EventArgs e)
	{
		if (!_hasDeserialisationSucceeded)
		{
			WidgetDialogService? widgetDialogServiceOrNull =
				_services?.GetRequiredService<WidgetDialogService>();

			if (widgetDialogServiceOrNull is not WidgetDialogService widgetDialogService)
			{
				throw new InvalidOperationException(
					Strings.ServiceProvider_Exception_FailedToLocateWidgetDialogService);
			}

			await widgetDialogService.ShowAlertAsync(
				AlertFlavour.Warning,
				Strings.Suspension_Exception_FailedToDeserialiseState_Friendly
			).ConfigureAwait(false);
		}
	}

	public void Dispose()
	{
		_services?.Dispose();
	}
}
