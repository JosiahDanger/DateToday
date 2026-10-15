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

internal sealed partial class WidgetView : Window, IScreenInformationProvider, IDisposable
{
	private readonly IPositionController _positionController;
	private Screen? _lastKnownScreen;
	private bool _isInitialPhysicalPositionApplied;

	public WidgetView(IPositionController positionController)
	{
		InitializeComponent();

		_positionController = positionController;
		_positionController.PropertyChanged += OnPositionConfigChanged;

		this.Closed += OnClosed;
		this.Loaded += OnLoaded;
	}

	/// <summary>
	/// Returns the <see cref="Screen" /> within which the specified
	///	<paramref name="logicalCoordinate" /> is enclosed.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no monitors are detected.
	/// </exception>

	public Screen GetScreenEnclosingLogicalCoordinate(Point logicalCoordinate)
	{
		PixelPoint enclosedPhysicalPosition =
			PixelPoint.FromPoint(logicalCoordinate, this.DesktopScaling);

		Screen? currentEnclosingScreen =
			Screens.All.FirstOrDefault(screen => screen.Bounds.Contains(enclosedPhysicalPosition));

		if (currentEnclosingScreen != null)
		{
			_lastKnownScreen = currentEnclosingScreen;
		}

		return
			currentEnclosingScreen
			?? _lastKnownScreen
			?? this.Screens.Primary
			?? throw new InvalidOperationException(
							Strings.WidgetView_Exception_NoAvailableMonitors);
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		this.Closed -= OnClosed;
		Dispose();
	}

	private void OnDoubleTapped(object? sender, TappedEventArgs e) =>
		_positionController.DoubleTappedCommand.Execute(null);

	/// <summary>
	/// Performs one-time initialisation: registers event handlers, and notifies the
	/// <see cref="IPositionController" /> that the window is loaded.
	/// </summary>
	/// <remarks>
	/// When the <see cref="IPositionController.LoadedCommand" /> is executed during initialisation,
	/// the IPositionController will anchor the selected window corner. However, the
	/// IPositionController will also anchor the selected window corner in response to the window's
	/// <see cref="Control.SizeChanged" /> event, which can be raised inadvertently should the
	/// window move to another monitor when it is assigned its initial physical position
	/// (see <see cref="OnPositionConfigChanged" />). It is redundant for this process to occur
	/// twice. Therefore, subscription to the SizeChanged event is delayed until the LoadedCommand
	/// has been executed.
	/// </remarks>

	private void OnLoaded(object? sender, RoutedEventArgs e)
	{
		this.Loaded -= OnLoaded;

		this.DoubleTapped += OnDoubleTapped;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
		this.Screens.Changed += OnScreensChanged;

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());

		_positionController.LoadedCommand.Execute(this.ClientSize);
		this.SizeChanged += OnSizeChanged;
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

	/// <summary>
	/// Updates the window's position in physical space based on changes to
	/// <see cref="IPositionController.WindowOriginLogicalPosition" />.
	/// </summary>
	/// <remarks>
	/// An edge-case has been observed to occur in multi-monitor configurations, but only before
	/// the window's initial physical position is applied. Consider that
	/// <see cref="WindowBase.DesktopScaling" /> will always return the scaling factor of the
	/// monitor on which the window is currently displayed, regardless of which monitor will later
	/// display the window. Before the window's initial position is applied,
	/// <see cref="OnPositionConfigChanged" /> determines explicitly the window's intended parent
	/// monitor, and uses its scaling factor to calculate the window's initial position.
	/// </remarks>

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		Point newLogicalPosition = _positionController.WindowOriginLogicalPosition;

		if (!_isInitialPhysicalPositionApplied)
		{
			Screen parentScreen = GetScreenEnclosingLogicalCoordinate(newLogicalPosition);

			PixelPoint newPhysicalPosition =
				PixelPoint.FromPoint(newLogicalPosition, parentScreen.Scaling);

			this.Position = newPhysicalPosition;

			_isInitialPhysicalPositionApplied = true;
		}
		else
		{
			this.Position = PixelPoint.FromPoint(newLogicalPosition, this.DesktopScaling);
		}
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
