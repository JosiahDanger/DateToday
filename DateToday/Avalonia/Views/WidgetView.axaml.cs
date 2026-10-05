using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.Avalonia.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;

namespace DateToday.Avalonia.Views;

/// <summary>
/// The <see cref="WidgetView" /> code-behind is responsible for updating in physical space the
/// on-screen position of the WidgetView window according to
/// <see cref="IPositionController.WindowOriginLogicalPosition" />. It does so with respect to the
/// configurable per-monitor scaling factor exposed by the operating system. In addition, the
/// IPositionController is notified by the code-behind of window-dragging operations conducted on
/// the WidgetView by the user.
/// </summary>
/// <remarks>
/// Avalonia measures logical space using a floating-point co-ordinate system, whereas the pixel
/// area occupied by the WidgetView window on the user's monitor exists in physical space.
/// </remarks>

internal sealed partial class WidgetView : Window, IParentScreenWorkingAreaProvider, IDisposable
{
	private readonly IPositionController _positionController;
	private Screen? _fallbackParentScreen;

	public WidgetView(IPositionController positionController)
	{
		InitializeComponent();

		_positionController = positionController;
		_positionController.PropertyChanged += OnPositionConfigChanged;

		this.Closed += OnClosed;
		this.Loaded += OnLoaded;
	}

	/// <summary>
	/// Returns the working area of the <see cref="Screen" /> within which the specified
	/// <paramref name="enclosedPoint" /> is currently enclosed.
	/// </summary>
	/// <remarks>
	/// The returned working area might be smaller than the Screen bounds in order to account for
	/// the taskbar, but this is handled by Avalonia.
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no monitors are detected.
	/// </exception>

	public Size GetParentScreenWorkingArea(Point enclosedPoint)
	{
		PixelPoint enclosedPixelPoint = PixelPoint.FromPoint(enclosedPoint, this.DesktopScaling);

		Screen? currentParentScreen =
			Screens.All.FirstOrDefault(screen => screen.Bounds.Contains(enclosedPixelPoint));

		if (currentParentScreen != null)
		{
			_fallbackParentScreen = currentParentScreen;
		}

		Screen parentScreen =
			currentParentScreen
			?? _fallbackParentScreen
			?? Screens.Primary
			?? throw new InvalidOperationException(
				Strings.WidgetView_Exception_NoAvailableMonitors);

		return parentScreen.WorkingArea.Size.ToSize(this.DesktopScaling);
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		this.Closed -= OnClosed;
		Dispose();
	}

	private void OnDoubleTapped(object? sender, TappedEventArgs e) =>
		_positionController.DoubleTappedCommand.Execute(null);

	private void OnLoaded(object? sender, RoutedEventArgs e)
	{
		this.Loaded -= OnLoaded;

		this.DoubleTapped += OnDoubleTapped;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
		this.Screens.Changed += OnScreensChanged;
		this.SizeChanged += OnSizeChanged;

		_positionController.LoadedCommand.Execute(this.ClientSize);

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.Properties.IsLeftButtonPressed)
		{
			_positionController.PointerPressedCommand.Execute(e.GetPosition(this));
		}
	}

	private void OnPointerMoved(object? sender, PointerEventArgs e) =>
		_positionController.PointerMovedCommand.Execute(e.GetPosition(this));

	private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
		_positionController.PointerReleasedCommand.Execute(null);

	private void OnScreensChanged(object? sender, EventArgs e) =>
		_positionController.ScreensChangedCommand.Execute(null);

	private void OnSizeChanged(object? sender, SizeChangedEventArgs e) =>
		_positionController.SizeChangedCommand.Execute(this.ClientSize);

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		this.Position =
			PixelPoint.FromPoint(
				_positionController.WindowOriginLogicalPosition, this.DesktopScaling);
	}

	public void Dispose()
	{
		this.DoubleTapped -= OnDoubleTapped;
		this.PointerPressed -= OnPointerPressed;
		this.PointerMoved -= OnPointerMoved;
		this.PointerReleased -= OnPointerReleased;
		this.Screens.Changed -= OnScreensChanged;
		this.SizeChanged -= OnSizeChanged;

		_positionController.PropertyChanged -= OnPositionConfigChanged;

		WeakReferenceMessenger.Default.Unregister<CloseApplicationMessage>(this);
	}
}
