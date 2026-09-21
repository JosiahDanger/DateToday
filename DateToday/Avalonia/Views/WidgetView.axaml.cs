using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
	private string? _cachedMonitorReference;
	private bool _cachedIsMouseDragEnabled, _isWindowDragAfoot;
	private Point _cursorPositionAtWindowDragStart;

	public WidgetView(IPositionProvider positionProvider)
	{
		InitializeComponent();

		_positionConfigProvider = positionProvider;

		PositionConfig initialPositionConfig = _positionConfigProvider.Position;

		_cachedMonitorReference = initialPositionConfig.MonitorReference;
		_cachedIsMouseDragEnabled = initialPositionConfig.IsMouseDragEnabled;

		this.Screens.Changed += OnScreensChanged;

		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;

		this.Closed += OnClosed;

		_positionConfigProvider.PropertyChanged += OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());

		FitWidgetToWorkingArea(initialPositionConfig.AnchoredCornerLogicalPosition);
		UpdateMouseDragToggleIcon();
	}

	/// <summary>
	/// Moves the widget such that it becomes enclosed entirely within the bounds of the desktop
	/// working area.
	/// </summary>
	/// <param name="currentLogicalPosition">The widget's logical position before constraint.</param>
	/// <returns>The new logical position corresponding to the pixel co-ordinate newly assigned by
	/// this method to the widget.</returns>

	private Point FitWidgetToWorkingArea(Point currentLogicalPosition)
	{
		static Point CalculateLogicalPositionMax(Size widgetSize, Size workingAreaSize)
		{
			double logicalPositionMaxX = workingAreaSize.Width - widgetSize.Width;
			double logicalPositionMaxY = workingAreaSize.Height - widgetSize.Height;

			return new(logicalPositionMaxX, logicalPositionMaxY);
		}

		static Point ConstrainLogicalPosition(
			Point currentLogicalPosition, Point logicalPositionMax)
		{
			double newPositionX =
				Math.Clamp(currentLogicalPosition.X, PixelPoint.Origin.X, logicalPositionMax.X);
			double newPositionY =
				Math.Clamp(currentLogicalPosition.Y, PixelPoint.Origin.Y, logicalPositionMax.Y);

			return new(newPositionX, newPositionY);
		}

		Screen parentMonitor =
			GetSelectedMonitor(_positionConfigProvider.Position.MonitorReference);

		Size workingAreaSize = parentMonitor.WorkingArea.Size.ToSize(this.DesktopScaling);

		Point logicalPositionMax = CalculateLogicalPositionMax(this.ClientSize, workingAreaSize);

		Point logicalPositionConstrained =
			ConstrainLogicalPosition(currentLogicalPosition, logicalPositionMax);

		this.Position = PixelPoint.FromPoint(logicalPositionConstrained, this.DesktopScaling);

		return logicalPositionConstrained;
	}

	private Screen GetSelectedMonitor(string? targetMonitorReference)
	{
		Screen primaryScreen =
			Screens.Primary
			?? throw new InvalidOperationException(
				Strings.WidgetView_Exception_NoAvailableMonitors);

		if (targetMonitorReference is null)
		{
			return primaryScreen;
		}

		return Screens.All.FirstOrDefault(monitor =>
			string.Equals(
				monitor.ToString(),
				targetMonitorReference,
				StringComparison.OrdinalIgnoreCase))
			?? primaryScreen;
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

	private void OnScreensChanged(object? sender, EventArgs e)
	{
		string? currentMonitorReference = Screens.ScreenFromWindow(this)?.ToString();

		bool hasParentMonitorChanged =
			!string.Equals(
				_cachedMonitorReference,
				currentMonitorReference,
				StringComparison.OrdinalIgnoreCase);

		if (hasParentMonitorChanged)
		{
			// TODO. ParentMonitorChangedMessage should receive the anchored corner logical position, not the overall logical position.

			Point currentLogicalPosition = this.Position.ToPoint(this.DesktopScaling);
			Point logicalPositionConstrained = FitWidgetToWorkingArea(currentLogicalPosition);

			WeakReferenceMessenger.Default.Send(
				new WidgetMonitorChangedMessage(currentMonitorReference, logicalPositionConstrained));

			_cachedMonitorReference = currentMonitorReference;
		}
	}

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (_cachedIsMouseDragEnabled != _positionConfigProvider.Position.IsMouseDragEnabled)
		{
			UpdateMouseDragToggleIcon();

			_cachedIsMouseDragEnabled = _positionConfigProvider.Position.IsMouseDragEnabled;
		}
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.Properties.IsLeftButtonPressed && _positionConfigProvider.Position.IsMouseDragEnabled)
		{
			_isWindowDragAfoot = true;
			_cursorPositionAtWindowDragStart = e.GetPosition(this);
		}
	}

	private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (_isWindowDragAfoot)
		{
			// TODO. WidgetDraggedMessage should receive the anchored corner logical position, not the overall logical position.

			Point currentLogicalPosition = this.Position.ToPoint(this.DesktopScaling);
			Point logicalPositionConstrained = FitWidgetToWorkingArea(currentLogicalPosition);

			WeakReferenceMessenger.Default.Send(
				new WidgetDraggedMessage(logicalPositionConstrained));
		}

		_isWindowDragAfoot = false;
	}

	private void OnPointerMoved(object? sender, PointerEventArgs e)
	{
		if (_isWindowDragAfoot)
		{
			Point currentCursorPosition = e.GetPosition(this);
			Point cursorPositionDelta = currentCursorPosition - _cursorPositionAtWindowDragStart;

			Point currentLogicalPosition = this.Position.ToPoint(this.DesktopScaling);
			Point newLogicalPosition = currentLogicalPosition + cursorPositionDelta;

			this.Position = PixelPoint.FromPoint(newLogicalPosition, this.DesktopScaling);
		}
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		this.Closed -= OnClosed;

		this.PointerPressed -= OnPointerPressed;
		this.PointerMoved -= OnPointerMoved;
		this.PointerReleased -= OnPointerReleased;

		this.Screens.Changed -= OnScreensChanged;

		WeakReferenceMessenger.Default.Unregister<CloseApplicationMessage>(this);
	}
}
