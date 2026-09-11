using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.Models;
using DateToday.Utilities;
using System;
using System.ComponentModel;
using System.Threading;

namespace DateToday.Avalonia.ViewModels;

/// <summary>
/// The primary purpose of WidgetViewModel is to reflect in the <see cref="WidgetView" /> via
/// unidirectional binding the current mutable state of the singleton <see cref="WidgetModel" />.
///
/// In WidgetViewModel, WidgetModel state is accessed through a read-only
/// <see cref="IWidgetModelSnapshot" />, which ensures at compile time that the WidgetViewModel does
/// not mutate the WidgetModel state, and thus eliminates the possible occurrence of binding
/// feedback loops.
/// </summary>

internal sealed partial class WidgetViewModel : ObservableObject, IDisposable
{
	private readonly IWidgetModelSnapshot _widgetModelSnapshot;
	private readonly bool _isPropertyInitialisationComplete;
	private Timer? _widgetContentUpdateScheduler;

	public WidgetViewModel(IWidgetModelSnapshot widgetModelSnapshot)
	{
		_widgetModelSnapshot = widgetModelSnapshot;
		_widgetModelSnapshot.PropertyChanged += OnWidgetModelPropertyChanged;

		Content = _widgetModelSnapshot.Content;
		Position = _widgetModelSnapshot.Position;
		Font = _widgetModelSnapshot.Font;
		App = _widgetModelSnapshot.App;

		_isPropertyInitialisationComplete = true;

		ResetWidgetContentUpdateScheduler();
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
	/// The string's format is determined by <see cref="DateTimeFormat" /> and
	/// <see cref="DateTimeCulture"/>.
	///
	/// The property's initial value is calculated upon access, and is subsequently updated at
	/// real-time clock boundaries specified by <see cref="RefreshIntervalSeconds" />.
	/// </summary>

	public string? DateTimeText => FormatCurrentDateTime();

	[RelayCommand]
	private void ToggleMouseDrag()
	{
		WeakReferenceMessenger.Default.Send(
			new ToggleMouseDragMessage(!Position.IsMouseDragEnabled));
	}

	[RelayCommand]
	private static void OpenSettingsView()
	{
		WeakReferenceMessenger.Default.Send(new OpenSettingsViewMessage());
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
	/// Leverages the <see cref="ActionScheduler"/> to create a timer that invokes property change
	/// notifications for <see cref="DateTimeText"/>.
	/// </summary>

	private void ResetWidgetContentUpdateScheduler()
	{
		_widgetContentUpdateScheduler?.Dispose();

		_widgetContentUpdateScheduler =
			ActionScheduler.Create(
				Content.RefreshIntervalSeconds,
				() => OnPropertyChanged(nameof(DateTimeText)));
	}

	private void OnWidgetModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(WidgetModel.Content):
				Content = _widgetModelSnapshot.Content;
				break;

			case nameof(WidgetModel.Position):
				Position = _widgetModelSnapshot.Position;
				break;

			case nameof(WidgetModel.Font):
				Font = _widgetModelSnapshot.Font;
				break;

			case nameof(WidgetModel.App):
				App = _widgetModelSnapshot.App;
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
			ResetWidgetContentUpdateScheduler();
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
		_widgetModelSnapshot.PropertyChanged -= OnWidgetModelPropertyChanged;
		_widgetContentUpdateScheduler?.Dispose();
	}
}
