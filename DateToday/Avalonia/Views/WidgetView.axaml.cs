using Avalonia;
using Avalonia.Controls;
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

internal sealed partial class WidgetView : Window, IParentScreenWorkingAreaProvider
{
	private readonly IPositionController _positionController;
	private Screen? _fallbackParentScreen;

	public WidgetView(IPositionController positionController)
	{
		InitializeComponent();

		_positionController = positionController;

		this.Loaded += OnLoaded;
		_positionController.PropertyChanged += OnPositionConfigChanged;
	}

	private void OnLoaded(object? sender, RoutedEventArgs e)
	{
		_positionController.LoadedCommand.Execute(this.ClientSize);

		this.PointerPressed += (_, eventArgs) =>
		{
			if (eventArgs.Properties.IsLeftButtonPressed)
			{
				_positionController.PointerPressedCommand.Execute(eventArgs.GetPosition(this));
			}
		};

		this.PointerMoved += (_, eventArgs) =>
			_positionController.PointerMovedCommand.Execute(eventArgs.GetPosition(this));

		this.PointerReleased += (_, _) => _positionController.PointerReleasedCommand.Execute(null);

		this.Screens.Changed += (_, _) => _positionController.ScreensChangedCommand.Execute(null);

		this.SizeChanged += (_, _) =>
			_positionController.SizeChangedCommand.Execute(this.ClientSize);

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());
	}

	/// <summary>
	/// Returns the working area of the <see cref="Screen"/> within which the specified
	/// <see cref="Point"/> is currently enclosed.
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

	private void OnPositionConfigChanged(object? sender, PropertyChangedEventArgs e)
	{
		this.Position =
			PixelPoint.FromPoint(
				_positionController.WindowOriginLogicalPosition, this.DesktopScaling);
	}
}
