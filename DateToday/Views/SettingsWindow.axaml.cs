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
    private readonly Control? _settingsWindowDragHandle, _settingsDateFormatField;
    private bool _isWindowDragInEffect;
    private Point _cursorPositionAtWindowDragStart;
#endif

    public SettingsWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Make the previewer happy.
            return;
        }

        _settingsDateFormatField = this.FindControl<TextBox>("SettingsDateFormatField");

#if OS_WINDOWS
        _settingsWindowDragHandle = this.FindControl<Border>("SettingsWindowDragHandle");
#endif

        this.WhenActivated(disposables =>
        {
            ViewModel!.CloseSettingsView
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(dialogResult => Close(dialogResult))
                      .DisposeWith(disposables);

            if (_settingsDateFormatField != null)
            {
                /* The TextBox 'SettingsDateFormatField' is initialised in XAML such that its
                 * 'undo' / 'redo' functionality is disabled. Upon activation of the Settings
                 * window, the control's 'undo' / 'redo' functionality is manually enabled here.
                 * 
                 * This behaviour being in place prevents a minor bug that would otherwise occur if
                 * the user were to perform an 'undo' operation on the TextBox immediately after
                 * opening the Settings window. When this bug occurs, text inside the control is
                 * erased entirely, and the validation message "Curly braces are not permitted" is
                 * displayed in error. */

                ((TextBox)_settingsDateFormatField).IsUndoEnabled = true;
            }
            
#if OS_WINDOWS
            if (_settingsWindowDragHandle != null)
            {
                Observable.FromEventPattern<PointerEventArgs>(
                    handler => _settingsWindowDragHandle.PointerMoved += handler,
                    handler => _settingsWindowDragHandle.PointerMoved -= handler,
                    RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => WindowDragHandle_OnPointerMoved(x.Sender, x.EventArgs))
                .DisposeWith(disposables);

                Observable.FromEventPattern<PointerPressedEventArgs>(
                    handler => _settingsWindowDragHandle.PointerPressed += handler,
                    handler => _settingsWindowDragHandle.PointerPressed -= handler,
                    RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => WindowDragHandle_OnPointerPressed(x.Sender, x.EventArgs))
                .DisposeWith(disposables);

                Observable.FromEventPattern<PointerReleasedEventArgs>(
                    handler => _settingsWindowDragHandle.PointerReleased += handler,
                    handler => _settingsWindowDragHandle.PointerReleased -= handler,
                    RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
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
        if ((e.Source is Control sourceControl) && (sourceControl == _settingsWindowDragHandle))
        {
            _isWindowDragInEffect = true;
            _cursorPositionAtWindowDragStart = e.GetPosition(this);
        }
    }

    private void WindowDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _isWindowDragInEffect = false;
#endif
}