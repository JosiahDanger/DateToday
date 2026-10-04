using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.PresentationServices;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.Views;
using DateToday.DomainServices;
using DateToday.Models;
using DateToday.Utilities;
using System;
using System.ComponentModel;
using System.Threading;

namespace DateToday.Avalonia.ViewModels;

/// <summary>
/// The primary purpose of WidgetViewModel is to reflect in the <see cref="WidgetView" /> via
/// unidirectional binding the current mutable state of the singleton <see cref="WidgetModel" />.
/// </summary>
/// <remarks>
/// In WidgetViewModel, WidgetModel state is accessed through a read-only
/// <see cref="IWidgetModelSnapshot" />, which ensures at compile time that the WidgetViewModel does
/// not mutate the WidgetModel state directly. The intent here is to prevent the possible occurence
/// of binding feedback loops.
/// </remarks>

internal sealed partial class WidgetViewModel : ObservableObject, IPositionController, IDisposable
{
	private readonly IWidgetModelSnapshot _widgetModelSnapshot;
	private readonly WidgetModelMutationService _widgetModelMutationService;
	private readonly Lazy<IParentScreenWorkingAreaProvider> _parentScreenWorkingAreaProvider;
	private readonly GeometryProvider _geometryProvider;
	private readonly bool _isPropertyInitialisationComplete;
	private Timer? _widgetContentUpdateScheduler;
	private bool _isWindowDragAfoot;
	private Point _cursorLogicalPositionAtWindowDragStart;
	private Size _cachedWindowSize;

	public WidgetViewModel(
		IWidgetModelSnapshot widgetModelSnapshot,
		WidgetModelMutationService widgetModelMutationService,
		Lazy<IParentScreenWorkingAreaProvider> parentScreenWorkingAreaProvider,
		GeometryProvider geometryProvider)
	{
		_widgetModelSnapshot = widgetModelSnapshot;
		_widgetModelMutationService = widgetModelMutationService;
		_parentScreenWorkingAreaProvider = parentScreenWorkingAreaProvider;
		_geometryProvider = geometryProvider;

		_widgetModelSnapshot.PropertyChanged += OnWidgetModelPropertyChanged;

		Content = _widgetModelSnapshot.Content;
		Position = _widgetModelSnapshot.Position;
		Font = _widgetModelSnapshot.Font;
		App = _widgetModelSnapshot.App;

		_isPropertyInitialisationComplete = true;

		ResetWidgetContentUpdateScheduler();
		MouseDragToggleIcon = GetNewMouseDragToggleIcon();
	}

	[ObservableProperty]
	public partial ContentConfig Content { get; private set; }

	[ObservableProperty]
	public partial PositionConfig Position { get; private set; }

	[ObservableProperty]
	public partial FontConfig Font { get; private set; }

	[ObservableProperty]
	public partial AppConfig App { get; private set; }

	/// <summary>
	/// Controls the logical position of the <see cref="WidgetView" /> window's origin.
	/// </summary>
	/// <remarks>
	/// Avalonia measures logical space using a floating-point co-ordinate system, whereas the pixel
	/// area occupied by the WidgetView window on the user's monitor exists in physical space.
	/// </remarks>

	[ObservableProperty]
	public partial Point WindowOriginLogicalPosition { get; private set; }

	[ObservableProperty]
	public partial StreamGeometry MouseDragToggleIcon { get; private set; }

	/// <summary>
	/// A calculated property that returns a string representation of the current date and/or time.
	/// The string's format is determined by <see cref="DateTimeFormat" /> and
	/// <see cref="DateTimeCulture" />.
	/// </summary>
	/// <remarks>
	/// The property's initial value is calculated upon access, and is subsequently updated at
	/// real-time clock boundaries specified by <see cref="RefreshIntervalSeconds" />.
	/// </remarks>

	public string? DateTimeText => FormatCurrentDateTime();

	[RelayCommand]
	private void Loaded(Size windowSize)
	{
		AnchorSelectedWindowCorner(windowSize);
	}

	[RelayCommand]
	private void PointerPressed(Point cursorLogicalPosition)
	{
		if (Position.IsMouseDragEnabled)
		{
			_isWindowDragAfoot = true;
			_cursorLogicalPositionAtWindowDragStart = cursorLogicalPosition;
		}
	}

	[RelayCommand]
	private void PointerMoved(Point cursorLogicalPosition)
	{
		if (_isWindowDragAfoot)
		{
			Point cursorLogicalPositionDelta =
				cursorLogicalPosition - _cursorLogicalPositionAtWindowDragStart;

			this.WindowOriginLogicalPosition += cursorLogicalPositionDelta;
		}
	}

	[RelayCommand]
	private void PointerReleased()
	{
		if (_isWindowDragAfoot)
		{
			FitWindowToWorkingArea();
			UpdateAnchoredCornerLogicalPosition();
		}

		_isWindowDragAfoot = false;
	}

	[RelayCommand]
	private void ScreensChanged()
	{
		FitWindowToWorkingArea();
		UpdateAnchoredCornerLogicalPosition();
	}

	[RelayCommand]
	private void SizeChanged(Size windowSize)
	{
		AnchorSelectedWindowCorner(windowSize);
	}

	[RelayCommand]
	private void ToggleMouseDrag()
	{
		_widgetModelMutationService.MutatePositionConfig(
			positionConfig =>
				positionConfig with
				{
					IsMouseDragEnabled = !this.Position.IsMouseDragEnabled
				});

		MouseDragToggleIcon = GetNewMouseDragToggleIcon();
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
	/// Applies the configured anchored corner position and fits the window to the working area.
	/// </summary>

	private void AnchorSelectedWindowCorner(Size windowSize)
	{
		_cachedWindowSize = windowSize;
		CornerIdentifier anchoredCorner = this.Position.AnchoredCorner;

		double offsetX =
			(anchoredCorner is CornerIdentifier.TopRight or CornerIdentifier.BottomRight)
			? _cachedWindowSize.Width
			: 0;

		double offsetY =
			(anchoredCorner is CornerIdentifier.BottomLeft or CornerIdentifier.BottomRight)
			? _cachedWindowSize.Height
			: 0;

		Vector offsetVector = new(offsetX, offsetY);

		this.WindowOriginLogicalPosition =
			this.Position.AnchoredCornerLogicalPosition - offsetVector;

		FitWindowToWorkingArea();
	}

	/// <summary>
	/// Calculates and commits to the <see cref="WidgetModel" /> the current logical position of the
	/// <see cref="WidgetView" /> window's anchored corner.
	/// </summary>

	private void UpdateAnchoredCornerLogicalPosition()
	{
		CornerIdentifier anchoredCorner = this.Position.AnchoredCorner;
		Point windowOriginLogicalPosition = this.WindowOriginLogicalPosition;
		Size windowSize = _cachedWindowSize;

		double originX = anchoredCorner switch
		{
			CornerIdentifier.TopRight or CornerIdentifier.BottomRight
				=> windowOriginLogicalPosition.X + windowSize.Width,
			_ => windowOriginLogicalPosition.X
		};

		double originY = anchoredCorner switch
		{
			CornerIdentifier.BottomLeft or CornerIdentifier.BottomRight
				=> windowOriginLogicalPosition.Y + windowSize.Height,
			_ => windowOriginLogicalPosition.Y
		};

		_widgetModelMutationService.MutatePositionConfig(
			positionConfig =>
				positionConfig with
				{
					AnchoredCornerLogicalPosition = new Point(originX, originY)
				});
	}

	/// <summary>
	/// Adjusts in logical space the current position of the <see cref="WidgetView" /> window such
	/// that it becomes enclosed entirely within its working area.
	/// </summary>

	private void FitWindowToWorkingArea()
	{
		Size parentScreenWorkingAreaSize =
			_parentScreenWorkingAreaProvider.Value.GetParentScreenWorkingArea(
				this.WindowOriginLogicalPosition);

		double logicalPositionMaxX =
			Math.Max(0, parentScreenWorkingAreaSize.Width - _cachedWindowSize.Width);

		double logicalPositionMaxY =
			Math.Max(0, parentScreenWorkingAreaSize.Height - _cachedWindowSize.Height);

		Point windowOriginLogicalPositionConstrained =
			new(
				Math.Clamp(this.WindowOriginLogicalPosition.X, 0, logicalPositionMaxX),
				Math.Clamp(this.WindowOriginLogicalPosition.Y, 0, logicalPositionMaxY));

		this.WindowOriginLogicalPosition = windowOriginLogicalPositionConstrained;
	}

	private StreamGeometry GetNewMouseDragToggleIcon()
	{
		string iconKey = this.Position.IsMouseDragEnabled
			? Strings.Icons_Key_Lock
			: Strings.Icons_Key_Unlock;

		return _geometryProvider.GetGeometry(iconKey);
	}

	/// <summary>
	/// Leverages the <see cref="ActionScheduler" /> to create a timer that invokes property change
	/// notifications for <see cref="DateTimeText" />.
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
				this.Content = _widgetModelSnapshot.Content;
				break;

			case nameof(WidgetModel.Position):
				this.Position = _widgetModelSnapshot.Position;
				break;

			case nameof(WidgetModel.Font):
				this.Font = _widgetModelSnapshot.Font;
				break;

			case nameof(WidgetModel.App):
				this.App = _widgetModelSnapshot.App;
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
