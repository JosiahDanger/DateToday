using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.ComponentModel;
using System.Reactive.Linq;

namespace DateToday.Avalonia.ViewModels;

/// <summary>
/// The primary purpose of WidgetViewModel is to reflect in <see cref="WidgetView" /> via
/// unidirectional binding the current mutable state of the singleton <see cref="WidgetModel" />. In
/// addition, for properties that may change independently of the WidgetModel, such as
/// MonitorReference, WidgetViewModel notifies the WidgetModel of changes via the
/// <see cref="WeakReferenceMessenger" />.
/// </summary>

internal sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
	private readonly WidgetModel _widgetModel;
	private readonly bool _isPropertyInitialisationComplete;
	private IDisposable? _timerSubscription;

	public WidgetViewModel(WidgetModel widgetModel)
	{
		_widgetModel = widgetModel;
		_widgetModel.PropertyChanged += OnWidgetModelPropertyChanged;

		Content = _widgetModel.Content;
		Position = _widgetModel.Position;
		Font = _widgetModel.Font;
		App = _widgetModel.App;

		_isPropertyInitialisationComplete = true;

		WeakReferenceMessenger.Default.Register<WidgetMonitorChangedMessage>(this,
			(_, message) =>
			{
				_widgetModel.Position =
					_widgetModel.Position with { MonitorReference = message.MonitorReference };
			});

		WeakReferenceMessenger.Default.Register<IsMouseDragEnabledChangedMessage>(this,
			(_, message) =>
			{
				_widgetModel.Position =
					_widgetModel.Position with { IsMouseDragEnabled = message.IsMouseDragEnabled };
			});

		ResetSecondTickObservable(Content.RefreshIntervalSeconds);
	}

	[ObservableProperty]
	public partial ContentConfig Content { get; set; }

	[ObservableProperty]
	public partial PositionConfig Position { get; set; }

	[ObservableProperty]
	public partial FontConfig Font { get; set; }

	[ObservableProperty]
	public partial AppConfig App { get; set; }

	/// <summary>
	/// A calculated property that returns a string representation of the current date and/or time.
	/// The string's format is determined by <see cref="DateTimeFormat"/> and
	/// <see cref="DateTimeCulture"/>.
	///
	/// The property's initial value is calculated upon access, and is subsequently updated at
	/// real-time clock boundaries specified by <see cref="RefreshIntervalSeconds"/>.
	/// </summary>

	public string? DateTimeText => FormatCurrentDateTime();

	[RelayCommand]
	private static void CloseApplication()
	{
		WeakReferenceMessenger.Default.Send(new CloseApplicationMessage());
	}

	private string FormatCurrentDateTime()
	{
		static string GetOrdinalDaySuffix(int ordinalDayOfMonth) =>
			ordinalDayOfMonth switch
			{
				1 or 21 or 31 => "st",
				2 or 22 => "nd",
				3 or 23 => "rd",
				_ => "th",
			};

		DateTime currentDateTime = DateTime.Now;

		if (Content.OrdinalDaySuffixPosition == null)
		{
			return currentDateTime.ToString(Content.DateTimeFormat, Content.DateTimeCulture);
		}

		if (Content.OrdinalDaySuffixPosition < 0)
		{
			throw new InvalidOperationException(
				Strings.WidgetViewModel_Exception_OrdinalDaySuffixPosition_Negative);
		}

		if (Content.OrdinalDaySuffixPosition > Content.DateTimeFormat.Length)
		{
			throw new InvalidOperationException(
				Strings.WidgetViewModel_Exception_OrdinalDaySuffixPosition_OutOfRange);
		}

		/* This code makes use of .NET composite formatting.
		 *
		 * See the following Microsoft Learn article:
		 * https://learn.microsoft.com/dotnet/standard/base-types/composite-formatting */

		int ordinalDayOfMonth = currentDateTime.Day;
		string ordinalDaySuffix = GetOrdinalDaySuffix(ordinalDayOfMonth);

		string dateTimeFormatIncludingSuffixPlaceholder =
			Content.DateTimeFormat.Insert((int)Content.OrdinalDaySuffixPosition, "{0}");

		string dateTimeStringIncludingSuffixPlaceholder =
			currentDateTime.ToString(
								dateTimeFormatIncludingSuffixPlaceholder,
								App.AppCulture);

		return string.Format(
							App.AppCulture,
							dateTimeStringIncludingSuffixPlaceholder,
							ordinalDaySuffix);
	}

	/// <summary>
	/// CreateSecondTickObservable(…) returns a timer observable that emits values at regular
	/// intervals, synchronised to real-time clock boundaries. Rather than starting immediately,
	/// it calculates the delay needed to align its first emission with the next interval
	/// boundary, then repeats at the specified refresh interval.
	/// </summary>

	private static IObservable<long> CreateSecondTickObservable(uint refreshIntervalSeconds)
	{
		if (refreshIntervalSeconds == 0)
		{
			throw new InvalidOperationException(
				Strings.WidgetViewModel_Exception_RefreshIntervalSeconds_Zero);
		}

		const double MillisecondsPerSecond = 1000;
		const double BoundaryDetectionToleranceMilliseconds = 0.5;

		double millisecondsPerInterval = refreshIntervalSeconds * MillisecondsPerSecond;

		return Observable.Defer(() =>
		{
			DateTime currentDateTime = DateTime.Now;

			long totalMillisecondsSinceEpoch = currentDateTime.Ticks / TimeSpan.TicksPerMillisecond;

			double elapsedInCycle = totalMillisecondsSinceEpoch % millisecondsPerInterval;

			double millisecondsPrecedingNextInterval =
				(millisecondsPerInterval - elapsedInCycle) % millisecondsPerInterval;

			/* Check if the next interval boundary is scheduled to occur right now, allowing for
			 * tolerance of floating-point precision loss. */

			if (Math.Abs(millisecondsPrecedingNextInterval) <
				BoundaryDetectionToleranceMilliseconds)
			{
				/* Reset the millisecond counter, such that the app will wait for the subsequent
				 * interval boundary to occur. This behaviour is intended to prevent a scenario
				 * in which two emissions are made in close succession of one another. */

				millisecondsPrecedingNextInterval = millisecondsPerInterval;
			}

			return Observable.Timer(
									TimeSpan.FromMilliseconds(millisecondsPrecedingNextInterval),
									TimeSpan.FromMilliseconds(millisecondsPerInterval));
		});
	}

	private void ResetSecondTickObservable(uint value)
	{
		_timerSubscription?.Dispose();

		_timerSubscription =
			CreateSecondTickObservable(value)
			.Subscribe(_ => OnPropertyChanged(nameof(DateTimeText)));
	}

	private void OnWidgetModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(WidgetModel.Content):
				Content = _widgetModel.Content;
				break;

			case nameof(WidgetModel.Position):
				Position = _widgetModel.Position;
				break;

			case nameof(WidgetModel.Font):
				Font = _widgetModel.Font;
				break;

			case nameof(WidgetModel.App):
				App = _widgetModel.App;
				break;
		}
	}

	partial void OnContentChanged(ContentConfig oldValue, ContentConfig newValue)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		if (newValue.RefreshIntervalSeconds != oldValue.RefreshIntervalSeconds)
		{
			ResetSecondTickObservable(newValue.RefreshIntervalSeconds);
		}
	}

	partial void OnAppChanged(AppConfig oldValue, AppConfig newValue)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		if (newValue.AppCulture != oldValue.AppCulture)
		{
			OnPropertyChanged(nameof(DateTimeText));
		}
	}

	public void Dispose()
	{
		WeakReferenceMessenger.Default.Unregister<WidgetMonitorChangedMessage>(this);
		WeakReferenceMessenger.Default.Unregister<IsMouseDragEnabledChangedMessage>(this);

		_widgetModel.PropertyChanged -= OnWidgetModelPropertyChanged;
		_timerSubscription?.Dispose();
	}
}
