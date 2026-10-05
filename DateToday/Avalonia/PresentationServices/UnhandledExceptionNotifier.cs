using Avalonia.Logging;
using Avalonia.Threading;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace DateToday.Avalonia.PresentationServices;

/// <summary>
/// Notifies the user via an alert dialog of unhandled exceptions arising from either the UI thread
/// or background asynchronous operations. If an alert dialog cannot be displayed, then Avalonia's
/// <see cref="ParametrizedLogger" /> is instead used as a fallback.
/// </summary>
/// <remarks>
/// This service will not function until <see cref="Initialise" /> is called.
/// </remarks>

internal sealed class UnhandledExceptionNotifier: IDisposable
{
	private static readonly CompositeFormat s_unhandledExceptionFormat =
		CompositeFormat.Parse(Strings.UnhandledExceptionNotifier_MessageTemplate);

	private bool _isDisposed;

	private readonly WidgetDialogService _widgetDialogService;

	private readonly DispatcherUnhandledExceptionEventHandler
		_userInterfaceThreadExceptionHandler;

	private readonly EventHandler<UnobservedTaskExceptionEventArgs>
		_asynchronousOperationExceptionHandler;

	public UnhandledExceptionNotifier(WidgetDialogService widgetDialogService)
	{
		_widgetDialogService = widgetDialogService;

		_userInterfaceThreadExceptionHandler =
			(_, eventArgs) => OnUserInterfaceThreadUnhandledException(eventArgs);

		_asynchronousOperationExceptionHandler =
			(_, eventArgs) => OnAsynchronousOperationUnhandledException(eventArgs);
	}

	/// <summary>
	/// Subscribes to exception events. This method is intended to be called during application
	/// startup.
	/// </summary>

	public UnhandledExceptionNotifier Initialise()
	{
		Dispatcher.UIThread.UnhandledException +=
			_userInterfaceThreadExceptionHandler;

		TaskScheduler.UnobservedTaskException +=
			_asynchronousOperationExceptionHandler;

		return this;
	}

	private void NotifyUser(string exceptionArea, Exception exception)
	{
		if (_isDisposed)
		{
			return;
		}

		string unhandledExceptionMessage =
			string.Format(
				CultureInfo.InvariantCulture,
				s_unhandledExceptionFormat,
				exceptionArea,
				exception.Message);

		_ =
			_widgetDialogService.ShowAlertAsync(
				AlertFlavour.Error,
				unhandledExceptionMessage
			).ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					ParametrizedLogger? logger =
						Logger.TryGet(LogEventLevel.Error, exceptionArea);

					logger?.Log(
						Strings.UnhandledExceptionNotifier_Logger_LogSource,
						exception.Message);
				}
			}, TaskScheduler.Default);
	}

	private void OnUserInterfaceThreadUnhandledException(
		DispatcherUnhandledExceptionEventArgs eventArgs)
	{
		NotifyUser(
			Strings.UnhandledExceptionNotifier_ExceptionArea_UserInterfaceThread,
			eventArgs.Exception);

		eventArgs.Handled = true;
	}

	private void OnAsynchronousOperationUnhandledException(
		UnobservedTaskExceptionEventArgs eventArgs)
	{
		NotifyUser(
			Strings.UnhandledExceptionNotifier_ExceptionArea_AsynchronousOperation,
			eventArgs.Exception);

		eventArgs.SetObserved();
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		Dispatcher.UIThread.UnhandledException -= _userInterfaceThreadExceptionHandler;

		TaskScheduler.UnobservedTaskException -= _asynchronousOperationExceptionHandler;
	}
}
