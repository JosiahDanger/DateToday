using CommunityToolkit.Mvvm.ComponentModel;
using DateToday.Avalonia.Resources;
using DateToday.Utilities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace DateToday.Models;

/// <summary>
/// WidgetModel is a singleton Model layer responsible for mutation of the desktop widget.
/// Properties are mutable by external classes only through
/// <see cref="WidgetModelMutationService" />; <see cref="IWidgetModelSnapshot" /> exposes them for
/// read-only access.
/// </summary>

internal sealed partial class WidgetModel : ObservableObject, IWidgetModelSnapshot, IDisposable
{
	private readonly bool _isPropertyInitialisationComplete;
	private Timer? _dateTimeTextUpdateScheduler;

	[ObservableProperty]
	public partial ContentConfig Content { get; internal set; }

	[ObservableProperty]
	public partial PositionConfig Position { get; internal set; }

	[ObservableProperty]
	public partial FontConfig Font { get; internal set; }

	[ObservableProperty]
	public partial AppConfig App { get; internal set; }

	[ObservableProperty]
	public partial string DateTimeText { get; private set; }

	public WidgetModel(
		ContentConfig content,
		PositionConfig position,
		FontConfig font,
		AppConfig app)
	{
		Content = content;
		Position = position;
		Font = font;
		App = app;

		DateTimeText = FormatCurrentDateTimeCore(Content.DateTimeFormat);

		_isPropertyInitialisationComplete = true;

		ResetWidgetContentUpdateScheduler();
	}

	partial void OnContentChanging(ContentConfig oldValue, ContentConfig newValue)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		if (newValue.DateTimeFormat != oldValue.DateTimeFormat)
		{
			DateTimeText = FormatCurrentDateTimeOrThrow(newValue.DateTimeFormat);
		}
	}

	partial void OnContentChanged(ContentConfig oldValue, ContentConfig newValue)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		if (!Equals(newValue.DateTimeCulture, oldValue.DateTimeCulture))
		{
			DateTimeText = FormatCurrentDateTimeCore(Content.DateTimeFormat);
		}

		if (newValue.RefreshIntervalSeconds != oldValue.RefreshIntervalSeconds)
		{
			ResetWidgetContentUpdateScheduler();
		}
	}

	/// <summary>
	/// Leverages the <see cref="ActionScheduler" /> to create a timer that invokes property change
	/// notifications for <see cref="DateTimeText" />.
	/// </summary>

	private void ResetWidgetContentUpdateScheduler()
	{
		_dateTimeTextUpdateScheduler?.Dispose();

		_dateTimeTextUpdateScheduler =
			ActionScheduler.Create(
				Content.RefreshIntervalSeconds,
				() => DateTimeText = FormatCurrentDateTimeCore(Content.DateTimeFormat));
	}

	private string FormatCurrentDateTimeOrThrow(string dateTimeFormat)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dateTimeFormat);

		try
		{
			return FormatCurrentDateTimeCore(dateTimeFormat);
		}
		catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
		{
			throw new ValidationException(Strings.WidgetModel_Exception_DateTimeFormat_Invalid, ex);
		}
	}

	private string FormatCurrentDateTimeCore(string dateTimeFormat)
	{
		DateTime currentDateTime = DateTime.Now;
		return currentDateTime.ToString(dateTimeFormat, Content.DateTimeCulture);
	}

	public void Dispose()
	{
		_dateTimeTextUpdateScheduler?.Dispose();
	}
}
