using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DateToday.Views;
internal partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
#if OS_WINDOWS
    private readonly Control? _windowDragHandle;
    private bool _isWindowDragInEffect;
    private Point _cursorPositionAtWindowDragStart;
#endif

    public SettingsWindow()
    {
        if (Design.IsDesignMode)
        {
            // Make the previewer happy.
            return;
        };

        InitializeComponent();

#if OS_WINDOWS
        _windowDragHandle = this.FindControl<Border>("WindowDragHandle");
#endif

        this.WhenActivated(disposables =>
        {
            ViewModel!.CommandCloseSettingsView.Subscribe(dialogResult => Close(dialogResult));

#if OS_WINDOWS
            if (_windowDragHandle != null)
            {
                Observable.FromEventPattern<PointerEventArgs>(
                    handler => _windowDragHandle.PointerMoved += handler,
                    handler => _windowDragHandle.PointerMoved -= handler,
                    RxApp.MainThreadScheduler)
                .Subscribe(x => WindowDragHandle_OnPointerMoved(x.Sender, x.EventArgs))
                .DisposeWith(disposables);

                Observable.FromEventPattern<PointerPressedEventArgs>(
                    handler => _windowDragHandle.PointerPressed += handler,
                    handler => _windowDragHandle.PointerPressed -= handler,
                    RxApp.MainThreadScheduler)
                .Subscribe(x => WindowDragHandle_OnPointerPressed(x.Sender, x.EventArgs))
                .DisposeWith(disposables);

                Observable.FromEventPattern<PointerReleasedEventArgs>(
                    handler => _windowDragHandle.PointerReleased += handler,
                    handler => _windowDragHandle.PointerReleased -= handler,
                    RxApp.MainThreadScheduler)
                .Subscribe(x => WindowDragHandle_OnPointerReleased(x.Sender, x.EventArgs))
                .DisposeWith(disposables);
            }
#endif
        });
    }

#if OS_WINDOWS
    private void WindowDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isWindowDragInEffect)
        {
            Point currentCursorPosition = e.GetPosition(this);
            Point cursorPositionDelta = currentCursorPosition - _cursorPositionAtWindowDragStart;

            Position = this.PointToScreen(cursorPositionDelta);
        }
    }

    private void WindowDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((e.Source is Control sourceControl) && (sourceControl == _windowDragHandle))
        {
            _isWindowDragInEffect = true;
            _cursorPositionAtWindowDragStart = e.GetPosition(this);
        }
    }

    private void WindowDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _isWindowDragInEffect = false;
#endif
}