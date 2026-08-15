using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.Globalization;
using System.Reactive.Linq;

namespace DateToday.Avalonia.ViewModels;

/// <summary>
/// WidgetViewModel is a passive consumer of the singleton WidgetModel. It will eventually reflect
/// in WidgetView all WidgetModel mutations.
/// </summary>

internal sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
	private readonly WidgetModel _activeModel;
	private IDisposable? _timerSubscription;

	[ObservableProperty]
	public partial string DateTimeFormat { get; set; }

	[ObservableProperty]
	public partial CultureInfo DateTimeCulture { get; set; }

	[ObservableProperty]
	public partial byte? OrdinalDaySuffixPosition { get; set; }

	[ObservableProperty]
	public partial uint RefreshIntervalSeconds { get; set; }

	[ObservableProperty]
	public partial WindowVertexIdentifier AnchoredCorner { get; set; }

	[ObservableProperty]
	public partial Point AnchoredCornerScaledPosition { get; set; }

	[ObservableProperty]
	public partial string? MonitorReference { get; set; }

	[ObservableProperty]
	public partial bool IsMouseDragEnabled { get; set; }

	[ObservableProperty]
	public partial FontFamily FontFamily { get; set; }

	[ObservableProperty]
	public partial int FontSize { get; set; }

	[ObservableProperty]
	public partial FontWeight FontWeight { get; set; }

	[ObservableProperty]
	public partial TextRenderingMode FontRenderingMode { get; set; }

	[ObservableProperty]
	public partial Color? CustomFontColour { get; set; }

	[ObservableProperty]
	public partial Color? CustomDropShadowColour { get; set; }

	/// <summary>
	/// A calculated property that returns a string representation of the current date and/or time.
	/// The string's format is determined by <see cref="DateTimeFormat"/> and
	/// <see cref="DateTimeCulture"/>.
	///
	/// The property's initial value is calculated upon access, and is subsequently updated at
	/// real-time clock boundaries specified by <see cref="RefreshIntervalSeconds"/>.
	/// </summary>

	public string? DateTimeText => FormatCurrentDateTime();

	public WidgetViewModel(WidgetModel wm)
	{
		_activeModel = wm;

		DateTimeFormat = _activeModel.Content.DateTimeFormat;
		DateTimeCulture = _activeModel.Content.DateTimeCulture;
		OrdinalDaySuffixPosition = _activeModel.Content.OrdinalDaySuffixPosition;
		RefreshIntervalSeconds = _activeModel.Content.RefreshIntervalSeconds;

		AnchoredCorner = _activeModel.Position.AnchoredCorner;
		AnchoredCornerScaledPosition = _activeModel.Position.AnchoredCornerScaledPosition;
		MonitorReference = _activeModel.Position.MonitorReference;
		IsMouseDragEnabled = _activeModel.Position.IsMouseDragEnabled;

		FontFamily = _activeModel.Font.FontFamily;
		FontSize = _activeModel.Font.FontSize;
		FontWeight = _activeModel.Font.FontWeight;
		FontRenderingMode = _activeModel.Font.FontRenderingMode;
		CustomFontColour = _activeModel.Font.CustomFontColour;
		CustomDropShadowColour = _activeModel.Font.CustomDropShadowColour;
	}

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

		if (OrdinalDaySuffixPosition == null)
		{
			return currentDateTime.ToString(DateTimeFormat, DateTimeCulture);
		}

		if (OrdinalDaySuffixPosition < 0)
		{
			throw new InvalidOperationException(
				Strings.WidgetViewModel_Exception_OrdinalDaySuffixPosition_Negative);
		}

		if (OrdinalDaySuffixPosition > DateTimeFormat.Length)
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
			DateTimeFormat.Insert((int)OrdinalDaySuffixPosition, "{0}");

		string dateTimeStringIncludingSuffixPlaceholder =
			currentDateTime.ToString(
								dateTimeFormatIncludingSuffixPlaceholder,
								DateTimeCulture);

		return string.Format(
							DateTimeCulture,
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

	partial void OnRefreshIntervalSecondsChanged(uint value)
	{
		_timerSubscription?.Dispose();

		_timerSubscription =
			CreateSecondTickObservable(value)
			.Subscribe(_ => OnPropertyChanged(nameof(DateTimeText)));
	}

	public void Dispose()
	{
		_timerSubscription?.Dispose();
	}
}
