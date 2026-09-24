using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.ViewModels;
using DateToday.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DateToday.Avalonia.Views;

internal sealed partial class WidgetView : Window
{
	private readonly IPositionProvider _positionConfigProvider;
	private CornerIdentifier _cachedAnchoredCorner;
	private bool _isWindowDragAfoot, _cachedIsMouseDragEnabled;
	private Point _cursorLogicalPositionAtWindowDragStart;

	public WidgetView(IPositionProvider positionProvider)
	{
		InitializeComponent();

		_positionConfigProvider = positionProvider;

		this.Closed += OnClosed;
		this.Loaded += OnLoaded;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
		this.Screens.Changed += OnScreensChanged;
		this.SizeChanged += OnSizeChanged;

		_positionConfigProvider.PropertyChanged += OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Register<ChangeWidgetMonitorMessage>(
			this, (_, message) => MoveWindowToSelectedScreen(message.ParentScreen));

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());
	}

	/// <summary>
	/// Gets the <see cref="Screen"/> that currently contains this window.
	/// </summary>
	/// <returns>
	/// The Screen whose bounds contain <see cref="Window.Position"/>.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no monitors are detected.
	/// </exception>

	private Screen ParentScreen =>
		Screens.All.FirstOrDefault(screen => screen.Bounds.Contains(this.Position))
		?? throw new InvalidOperationException(
			Strings.WidgetView_Exception_NoAvailableMonitors);

	/// <summary>
	/// Exposes the logical position the window's origin.
	/// </summary>
	/// <remarks>
	/// Avalonia measures logical space using a floating-point co-ordinate system, whereas the pixel
	/// area occupied by the window on the user's monitor exists in physical space.
	/// <see cref="Window.Position"/> returns the window's position in physical space, using integer
	/// co-ordinates.
	/// </remarks>
	private Point WindowOriginLogicalPosition
	{
		get { return this.Position.ToPoint(this.DesktopScaling); }
		set { this.Position = PixelPoint.FromPoint(value, this.DesktopScaling); }
	}

	/// <summary>
	/// Exposes the logical position of the window's anchored corner.
	/// </summary>
	/// <remarks>
	/// The anchored corner is determined by <see cref="_positionConfigProvider"/>.
	/// <para>
	/// <strong>Getter:</strong>
	/// The anchored corner's logical position is calculated using the window origin and size,
	/// according to which corner is anchored (top-left, top-right, bottom-left, or bottom-right).
	/// </para>
	/// <para>
	/// <strong>Setter:</strong>
	/// Accepts a desired anchored corner logical position and assigns to the window origin a
	/// corresponding logical position. Consider calling <see cref="FitWindowToWorkingArea"/> after
	/// using this setter.</para>
	/// </remarks>
	/// <value>A <see cref="Point"/> representing the anchored corner's logical position.</value>

	private Point AnchoredCornerLogicalPosition
	{
		get
		{
			CornerIdentifier anchoredCorner = _positionConfigProvider.Position.AnchoredCorner;
			Point windowOriginLogicalPosition = this.WindowOriginLogicalPosition;
			Size windowSize = this.ClientSize;

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

			return new Point(originX, originY);
		}

		set
		{
			CornerIdentifier anchoredCorner = _positionConfigProvider.Position.AnchoredCorner;
			Size windowSize = this.ClientSize;

			double offsetX =
				(anchoredCorner is CornerIdentifier.TopRight or CornerIdentifier.BottomRight)
				? windowSize.Width
				: 0;

			double offsetY =
				(anchoredCorner is CornerIdentifier.BottomLeft or CornerIdentifier.BottomRight)
				? windowSize.Height
				: 0;

			Vector offsetVector = new(offsetX, offsetY);

			this.WindowOriginLogicalPosition = value - offsetVector;
		}
	}

	/// <summary>
	/// Applies the configured anchored corner position and fits the window to the working area.
	/// </summary>

	private void AnchorSelectedWindowCorner()
	{
		this.AnchoredCornerLogicalPosition =
			_positionConfigProvider.Position.AnchoredCornerLogicalPosition;

		FitWindowToWorkingArea();
	}

	/// <summary>
	/// Adjusts in logical space the current position of the window such that it becomes enclosed
	/// entirely within its working area.
	/// </summary>

	private void FitWindowToWorkingArea()
	{
		Point windowOriginCurrentLogicalPosition = this.WindowOriginLogicalPosition;

		Size windowSize = this.ClientSize;

		Size parentScreenWorkingAreaSize =
			this.ParentScreen.WorkingArea.Size.ToSize(this.DesktopScaling);

		double logicalPositionMaxX =
			Math.Max(0, parentScreenWorkingAreaSize.Width - windowSize.Width);

		double logicalPositionMaxY =
			Math.Max(0, parentScreenWorkingAreaSize.Height - windowSize.Height);

		Point windowOriginLogicalPositionConstrained =
			new(
				Math.Clamp(windowOriginCurrentLogicalPosition.X, 0, logicalPositionMaxX),
				Math.Clamp(windowOriginCurrentLogicalPosition.Y, 0, logicalPositionMaxY));

		this.WindowOriginLogicalPosition = windowOriginLogicalPositionConstrained;
	}

	private void UpdateMouseDragToggleIcon()
	{
		string iconKey =
			_positionConfigProvider.Position.IsMouseDragEnabled
			? Strings.Icons_Key_Lock : Strings.Icons_Key_Unlock;

		if (this.FindResource(iconKey) is not StreamGeometry icon)
		{
			throw new KeyNotFoundException(
				Strings.WidgetView_Exception_FailedToLocateMouseDragToggleIcon);
		}

		IsMouseDragEnabledToggleIcon.Data = icon;
	}

	/// <summary>
	/// Moves the window to the specified <see cref="Screen"/>. The existing distance between the
	/// window's anchored corner and the corresponding Screen corner is maintained.
	/// </summary>
	/// <remarks>
	/// This method repositions the window by its anchored corner such that the window is pinned to
	/// the same relative position on the target screen. The window is subsequently fitted to the
	/// target screen's working area. The overall change in position is broadcast to listeners.
	/// </remarks>
	/// <param name="targetScreen">The Screen to which the window should be moved.</param>

	private void MoveWindowToSelectedScreen(Screen targetScreen)
	{
		Point CalculateScreenCornerLogicalPosition(Screen screen, CornerIdentifier corner) =>
			corner switch
			{
				CornerIdentifier.TopLeft =>
					screen.Bounds.TopLeft.ToPoint(this.DesktopScaling),
				CornerIdentifier.TopRight =>
					screen.Bounds.TopRight.ToPoint(this.DesktopScaling),
				CornerIdentifier.BottomRight =>
					screen.Bounds.BottomRight.ToPoint(this.DesktopScaling),
				CornerIdentifier.BottomLeft =>
					screen.Bounds.BottomLeft.ToPoint(this.DesktopScaling),
				_ => throw new ArgumentException(
					Strings.WidgetView_Exception_CornerIdentifierNotSupported)
			};

		CornerIdentifier anchoredCorner = _positionConfigProvider.Position.AnchoredCorner;

		Point currentScreenCornerLogicalPosition =
			CalculateScreenCornerLogicalPosition(this.ParentScreen, anchoredCorner);

		Point targetScreenCornerLogicalPosition =
			CalculateScreenCornerLogicalPosition(targetScreen, anchoredCorner);

		Point windowAnchoredCornerLogicalScreenOffset =
			this.AnchoredCornerLogicalPosition - currentScreenCornerLogicalPosition;

		this.AnchoredCornerLogicalPosition =
			targetScreenCornerLogicalPosition + windowAnchoredCornerLogicalScreenOffset;

		FitWindowToWorkingArea();

		WeakReferenceMessenger.Default.Send(
			new WidgetPositionUpdatedMessage(this.AnchoredCornerLogicalPosition));
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		this.Closed -= OnClosed;
		this.Loaded -= OnLoaded;
		this.PointerPressed -= OnPointerPressed;
		this.PointerMoved -= OnPointerMoved;
		this.PointerReleased -= OnPointerReleased;
		this.Screens.Changed -= OnScreensChanged;
		this.SizeChanged -= OnSizeChanged;

		_positionConfigProvider.PropertyChanged -= OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Unregister<ChangeWidgetMonitorMessage>(this);
		WeakReferenceMessenger.Default.Unregister<CloseApplicationMessage>(this);
	}

	private void OnLoaded(object? sender, RoutedEventArgs e)
	{
		_cachedAnchoredCorner = _positionConfigProvider.Position.AnchoredCorner;
		_cachedIsMouseDragEnabled = _positionConfigProvider.Position.IsMouseDragEnabled;

		AnchorSelectedWindowCorner();
		UpdateMouseDragToggleIcon();
	}

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		PositionConfig newPositionConfig = _positionConfigProvider.Position;

		if (_cachedAnchoredCorner != newPositionConfig.AnchoredCorner)
		{
			_cachedAnchoredCorner = newPositionConfig.AnchoredCorner;
			AnchorSelectedWindowCorner();
		}

		if (_cachedIsMouseDragEnabled != newPositionConfig.IsMouseDragEnabled)
		{
			_cachedIsMouseDragEnabled = newPositionConfig.IsMouseDragEnabled;
			UpdateMouseDragToggleIcon();
		}
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.Properties.IsLeftButtonPressed && _positionConfigProvider.Position.IsMouseDragEnabled)
		{
			_isWindowDragAfoot = true;
			_cursorLogicalPositionAtWindowDragStart = e.GetPosition(this);
		}
	}

	private void OnPointerMoved(object? sender, PointerEventArgs e)
	{
		if (_isWindowDragAfoot)
		{
			Point cursorLogicalPositionDelta =
				e.GetPosition(this) - _cursorLogicalPositionAtWindowDragStart;

			this.WindowOriginLogicalPosition += cursorLogicalPositionDelta;
		}
	}

	private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (_isWindowDragAfoot)
		{
			FitWindowToWorkingArea();

			WeakReferenceMessenger.Default.Send(
				new WidgetPositionUpdatedMessage(this.AnchoredCornerLogicalPosition));
		}

		_isWindowDragAfoot = false;
	}

	private void OnScreensChanged(object? sender, EventArgs e)
	{
		FitWindowToWorkingArea();

		WeakReferenceMessenger.Default.Send(
			new WidgetPositionUpdatedMessage(this.AnchoredCornerLogicalPosition));
	}

	private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		AnchorSelectedWindowCorner();
	}
}
