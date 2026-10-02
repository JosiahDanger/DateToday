using Avalonia;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace DateToday.Avalonia.ViewModels;

internal interface IPositionController : INotifyPropertyChanged
{
	Point WindowOriginLogicalPosition { get; }

	IRelayCommand<Size> LoadedCommand { get; }
	IRelayCommand<Point> PointerPressedCommand { get; }
	IRelayCommand<Point> PointerMovedCommand { get; }
	IRelayCommand PointerReleasedCommand { get; }
	IRelayCommand ScreensChangedCommand { get; }
	IRelayCommand<Size> SizeChangedCommand { get; }
}
