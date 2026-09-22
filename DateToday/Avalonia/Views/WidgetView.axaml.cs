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
	private bool _isWindowDragAfoot, _cachedIsMouseDragEnabled;
	private Point _cursorLogicalPositionAtWindowDragStart;

	public WidgetView(IPositionProvider positionProvider)
	{
		InitializeComponent();

		_positionConfigProvider = positionProvider;
		_cachedIsMouseDragEnabled = _positionConfigProvider.Position.IsMouseDragEnabled;

		this.Closed += OnClosed;
		this.Loaded += OnLoaded;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
		this.SizeChanged += OnSizeChanged;

		_positionConfigProvider.PropertyChanged += OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());

		UpdateMouseDragToggleIcon();
	}

	private Point LogicalPosition
	{
		get { return this.Position.ToPoint(this.DesktopScaling); }
		set { this.Position = PixelPoint.FromPoint(value, this.DesktopScaling); }
	}

	private Screen ParentScreen
	{
		get
		{
			return
				Screens.All.FirstOrDefault(x => x.Bounds.Contains(this.Position))
				?? throw new InvalidOperationException(
					Strings.WidgetView_Exception_NoAvailableMonitors);
		}
	}

	private static Point CalculateWindowOriginLogicalPositionFromVertex(
		WindowVertexIdentifier corner, Point cornerLogicalPosition, Size windowSize)
	{
		double originX = corner switch
		{
			WindowVertexIdentifier.TopRight or WindowVertexIdentifier.BottomRight
				=> cornerLogicalPosition.X - windowSize.Width,
			_ => cornerLogicalPosition.X
		};

		double originY = corner switch
		{
			WindowVertexIdentifier.BottomLeft or WindowVertexIdentifier.BottomRight
				=> cornerLogicalPosition.Y - windowSize.Height,
			_ => cornerLogicalPosition.Y
		};

		return new Point(originX, originY);
	}

	private static Point CalculateWindowVertexLogicalPositionFromOrigin(
		WindowVertexIdentifier corner, Point cornerLogicalPosition, Size windowSize)
	{
		double originX = corner switch
		{
			WindowVertexIdentifier.TopRight or WindowVertexIdentifier.BottomRight
				=> cornerLogicalPosition.X + windowSize.Width,
			_ => cornerLogicalPosition.X
		};

		double originY = corner switch
		{
			WindowVertexIdentifier.BottomLeft or WindowVertexIdentifier.BottomRight
				=> cornerLogicalPosition.Y + windowSize.Height,
			_ => cornerLogicalPosition.Y
		};

		return new Point(originX, originY);
	}

	private static Point FitWindowToWorkingArea(
		Point windowOriginLogicalPosition, Size windowSize, Size workingArea)
	{
		double logicalPositionMaxX = Math.Max(0, workingArea.Width - windowSize.Width);
		double logicalPositionMaxY = Math.Max(0, workingArea.Height - windowSize.Height);

		return new(
			Math.Clamp(windowOriginLogicalPosition.X, 0, logicalPositionMaxX),
			Math.Clamp(windowOriginLogicalPosition.Y, 0, logicalPositionMaxY));
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

	private void OnClosed(object? sender, EventArgs e)
	{
		this.Closed -= OnClosed;
		this.Loaded -= OnLoaded;
		this.PointerPressed -= OnPointerPressed;
		this.PointerMoved -= OnPointerMoved;
		this.PointerReleased -= OnPointerReleased;
		this.SizeChanged -= OnSizeChanged;

		_positionConfigProvider.PropertyChanged -= OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Unregister<CloseApplicationMessage>(this);
	}

	private void OnLoaded(object? sender, RoutedEventArgs e)
	{
		Point widgetOriginInitialLogicalPosition =
			CalculateWindowOriginLogicalPositionFromVertex(
				_positionConfigProvider.Position.AnchoredCorner,
				_positionConfigProvider.Position.AnchoredCornerLogicalPosition,
				this.ClientSize);

		this.LogicalPosition =
			FitWindowToWorkingArea(
				widgetOriginInitialLogicalPosition,
				this.ClientSize,
				this.ParentScreen.WorkingArea.Size.ToSize(this.DesktopScaling));
	}

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		bool newIsMouseDragEnabled = _positionConfigProvider.Position.IsMouseDragEnabled;

		if (_cachedIsMouseDragEnabled !=
			newIsMouseDragEnabled)
		{
			_cachedIsMouseDragEnabled = newIsMouseDragEnabled;
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

			Point newWidgetOriginLogicalPosition =
				this.LogicalPosition + cursorLogicalPositionDelta;

			this.LogicalPosition =
				FitWindowToWorkingArea(
					newWidgetOriginLogicalPosition,
					this.ClientSize,
					this.ParentScreen.WorkingArea.Size.ToSize(this.DesktopScaling));
		}
	}

	private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (_isWindowDragAfoot)
		{
			Point widgetAnchoredCornerLogicalPosition
				= CalculateWindowVertexLogicalPositionFromOrigin(
					_positionConfigProvider.Position.AnchoredCorner,
					this.LogicalPosition,
					this.ClientSize);

			WeakReferenceMessenger.Default.Send(
				new WidgetDraggedMessage(widgetAnchoredCornerLogicalPosition));
		}

		_isWindowDragAfoot = false;
	}

	private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		double widthDelta = e.NewSize.Width - e.PreviousSize.Width;
		double heightDelta = e.NewSize.Height - e.PreviousSize.Height;

		(double offsetX, double offsetY) =
			_positionConfigProvider.Position.AnchoredCorner switch
			{
				WindowVertexIdentifier.TopRight => (-widthDelta, 0),
				WindowVertexIdentifier.BottomLeft => (0, -heightDelta),
				WindowVertexIdentifier.BottomRight => (-widthDelta, -heightDelta),
				_ => (0, 0)
			};

		Point currentLogicalPosition = this.LogicalPosition;

		this.LogicalPosition =
			FitWindowToWorkingArea(
				new(currentLogicalPosition.X + offsetX, currentLogicalPosition.Y + offsetY),
				this.ClientSize,
				this.ParentScreen.WorkingArea.Size.ToSize(this.DesktopScaling));
	}
}
