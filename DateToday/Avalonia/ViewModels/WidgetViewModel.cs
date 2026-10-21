using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
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
using System.Linq;
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
	private readonly Lazy<IScreenInformationProvider> _screenInformationProvider;
	private readonly GeometryProvider _geometryProvider;
	private readonly bool _isPropertyInitialisationComplete;
	private Timer? _widgetContentUpdateScheduler;
	private bool _isWindowDragAfoot;
	private Point _cursorLogicalPositionAtWindowDragStart;
	private Size _cachedWindowSize;

	public WidgetViewModel(
		IWidgetModelSnapshot widgetModelSnapshot,
		WidgetModelMutationService widgetModelMutationService,
		Lazy<IScreenInformationProvider> screenInformationProvider,
		GeometryProvider geometryProvider)
	{
		_widgetModelSnapshot = widgetModelSnapshot;
		_widgetModelMutationService = widgetModelMutationService;
		_screenInformationProvider = screenInformationProvider;
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
	/// The string's format is determined by <see cref="ContentConfig.DateTimeFormat" /> and
	/// <see cref="ContentConfig.DateTimeCulture" />.
	/// </summary>
	/// <remarks>
	/// The property's initial value is calculated upon access, and is subsequently updated at
	/// real-time clock boundaries specified by <see cref="ContentConfig.RefreshIntervalSeconds" />.
	/// </remarks>

	public string? DateTimeText => FormatCurrentDateTime();

	[RelayCommand]
	private void DoubleTapped()
	{
		_isWindowDragAfoot = false;
		WeakReferenceMessenger.Default.Send(new OpenSettingsViewMessage());
	}

	[RelayCommand]
	private void Loaded(Size windowSize)
	{
		_cachedWindowSize = windowSize;
		AnchorSelectedWindowCorner();
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
		if (!_isWindowDragAfoot)
		{
			/* The window's SizeChanged event can be raised when it is dragged between monitors that
			 * use differing scaling factors. Under these circumstances, its position doesn't need
			 * to be adjusted until the pointer is released. */

			_cachedWindowSize = windowSize;
			AnchorSelectedWindowCorner();
		}
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
		//static string GetOrdinalDaySuffix(int ordinalDayOfMonth) =>
		//	ordinalDayOfMonth switch
		//	{
		//		1 or 21 or 31 => "st",
		//		2 or 22 => "nd",
		//		3 or 23 => "rd",
		//		_ => "th",
		//	};

		DateTime currentDateTime = DateTime.Now;

		return currentDateTime.ToString(Content.DateTimeFormat, Content.DateTimeCulture);

		//if (!Content.IsOrdinalDaySuffixEnabled)
		//{
		//	return currentDateTime.ToString(Content.DateTimeFormat, Content.DateTimeCulture);
		//}

		/* This code makes use of .NET composite formatting.
		 *
		 * See the following Microsoft Learn article:
		 * https://learn.microsoft.com/dotnet/standard/base-types/composite-formatting */

		//int ordinalDayOfMonth = currentDateTime.Day;
		//string ordinalDaySuffix = GetOrdinalDaySuffix(ordinalDayOfMonth);

		//string dateTimeFormatIncludingSuffixPlaceholder =
		//	Content.DateTimeFormat.Insert((int)Content.OrdinalDaySuffixPosition, "{0}");

		//string dateTimeStringIncludingSuffixPlaceholder =
		//	currentDateTime.ToString(
		//						dateTimeFormatIncludingSuffixPlaceholder,
		//						App.AppCulture);

		//return string.Format(
		//					App.AppCulture,
		//					dateTimeStringIncludingSuffixPlaceholder,
		//					ordinalDaySuffix);
	}

	/// <summary>
	/// When the <see cref="WidgetView" /> window changes in size, this method may be called to
	/// adjust its position in logical space such that the
	/// <see cref="PositionConfig.AnchoredCorner" /> pinned by the user again becomes anchored to
	/// its intended co-ordinate.
	/// </summary>
	/// <remarks>
	/// Note that <see cref="FitWindowToWorkingArea" /> is called by this method after anchoring the
	/// selected corner. This behaviour is intended as a safeguard to prevent the widget from moving
	/// outside its <see cref="Screen.WorkingArea" />.
	/// </remarks>

	private void AnchorSelectedWindowCorner()
	{
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
	/// Calculates the logical position of the <see cref="WidgetView" /> window's specified corner.
	/// </summary>
	/// <param name="corner">
	/// The <see cref="CornerIdentifier" /> of the corner whose logical position will be calculated.
	/// </param>

	private Point CalculateWindowCornerLogicalPosition(CornerIdentifier corner)
	{
		Point windowOriginLogicalPosition = this.WindowOriginLogicalPosition;

		double cornerX = corner switch
		{
			CornerIdentifier.TopRight or CornerIdentifier.BottomRight
				=> windowOriginLogicalPosition.X + _cachedWindowSize.Width,
			_ => windowOriginLogicalPosition.X
		};

		double cornerY = corner switch
		{
			CornerIdentifier.BottomLeft or CornerIdentifier.BottomRight
				=> windowOriginLogicalPosition.Y + _cachedWindowSize.Height,
			_ => windowOriginLogicalPosition.Y
		};

		return new Point(cornerX, cornerY);
	}

	/// <summary>
	/// Commits to the <see cref="WidgetModel" /> the current logical position of the
	/// <see cref="WidgetView" /> window's <see cref="PositionConfig.AnchoredCorner" />.
	/// </summary>

	private void UpdateAnchoredCornerLogicalPosition()
	{
		_widgetModelMutationService.MutatePositionConfig(
			positionConfig =>
				positionConfig with
				{
					AnchoredCornerLogicalPosition =
						CalculateWindowCornerLogicalPosition(this.Position.AnchoredCorner)
				});
	}

	/// <summary>
	/// Adjusts in logical space the position of the <see cref="WidgetView" /> window such that it
	/// becomes enclosed entirely within its current <see cref="Screen.WorkingArea" />.
	/// </summary>
	/// <remarks>
	/// A target WorkingArea is inferred from the WidgetView window corner opposite the selected
	/// <see cref="PositionConfig.AnchoredCorner" />. The intent in doing so is to make an educated
	/// guess, in the event that multiple monitors are connected, as to which monitor the user
	/// expects to display the widget.
	/// </remarks>

	private void FitWindowToWorkingArea()
	{
		static CornerIdentifier GetOppositeCorner(CornerIdentifier corner)
		{
			return corner switch
			{
				CornerIdentifier.TopLeft => CornerIdentifier.BottomRight,
				CornerIdentifier.TopRight => CornerIdentifier.BottomLeft,
				CornerIdentifier.BottomLeft => CornerIdentifier.TopRight,
				CornerIdentifier.BottomRight => CornerIdentifier.TopLeft,
				_ => throw new ArgumentException(
						Strings.WidgetViewModel_Exception_CornerIdentifierNotSupported)
			};
		}

		CornerIdentifier anchoredCorner = this.Position.AnchoredCorner;
		CornerIdentifier oppositeCorner = GetOppositeCorner(anchoredCorner);

		Point oppositeCornerLogicalPosition = CalculateWindowCornerLogicalPosition(oppositeCorner);

		Screen parentScreen =
			_screenInformationProvider
			.Value
			.GetScreenEnclosingLogicalCoordinate(oppositeCornerLogicalPosition);

		Rect parentScreenWorkingArea = parentScreen.WorkingArea.ToRect(parentScreen.Scaling);

		double logicalPositionMinX = parentScreenWorkingArea.Left;
		double logicalPositionMaxX = parentScreenWorkingArea.Right - _cachedWindowSize.Width;

		double logicalPositionMinY = parentScreenWorkingArea.Top;
		double logicalPositionMaxY = parentScreenWorkingArea.Bottom - _cachedWindowSize.Height;

		double constrainedX =
			Math.Clamp(
				this.WindowOriginLogicalPosition.X, logicalPositionMinX, logicalPositionMaxX);

		double constrainedY =
			Math.Clamp(
				this.WindowOriginLogicalPosition.Y, logicalPositionMinY, logicalPositionMaxY);

		Point windowOriginLogicalPositionConstrained = new(constrainedX, constrainedY);

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

	partial void OnPositionChanged(PositionConfig oldValue, PositionConfig newValue)
	{
		if (!_isPropertyInitialisationComplete)
		{
			return;
		}

		if (_isWindowDragAfoot)
		{
			return;
		}

		bool hasAnchoredCornerChanged =
			newValue.AnchoredCorner != oldValue.AnchoredCorner;

		bool hasAnchoredCornerLogicalPositionChanged =
			newValue.AnchoredCornerLogicalPosition != oldValue.AnchoredCornerLogicalPosition;

		bool hasIsMouseDragEnabledChanged =
			newValue.IsMouseDragEnabled != oldValue.IsMouseDragEnabled;

		int propertyChangeCount =
			new[]
			{
				hasAnchoredCornerChanged,
				hasAnchoredCornerLogicalPositionChanged,
				hasIsMouseDragEnabledChanged
			}
			.Count(hasChanged => hasChanged);

		if (propertyChangeCount > 1)
		{
			throw new InvalidOperationException(
						Strings.WidgetViewModel_Exception_PositionConfigSimultaneousChanges);
		}

		if (hasAnchoredCornerChanged)
		{
			AnchorSelectedWindowCorner();
			UpdateAnchoredCornerLogicalPosition();
		}
		else if (hasAnchoredCornerLogicalPositionChanged)
		{
			Vector translationVector =
				newValue.AnchoredCornerLogicalPosition - oldValue.AnchoredCornerLogicalPosition;

			this.WindowOriginLogicalPosition += translationVector;
		}
		else if (hasIsMouseDragEnabledChanged)
		{
			MouseDragToggleIcon = GetNewMouseDragToggleIcon();
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
