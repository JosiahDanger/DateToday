using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DateToday.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace DateToday.Views;

internal sealed partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
    const string UNICODE_CANCELLATION_X = "\xd83d\xddd9";

#if OS_WINDOWS
    private bool _isWindowDragInEffect, _isWindowDragPrevented;
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

        this.WhenActivated(disposables =>
        {
            if (DesktopScaling == 1)
            {
                /* Only show text inside the exit button when the operating system display scaling 
                 * factor is equal to 100%. Otherwise, the text won't be centered properly. */

                ViewModel!.SettingsExitButtonContent = UNICODE_CANCELLATION_X;
            }

            ViewModel!.CloseWidgetSettings
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => Close())
                      .DisposeWith(disposables);

            /* The TextBox 'SettingsDateFormatField' is initialised in XAML such that its 
             * 'undo' / 'redo' functionality is disabled. Upon activation of the Settings window, 
             * the control's 'undo' / 'redo' functionality is manually enabled here. This behaviour 
             * being in place prevents a minor bug that would otherwise occur if the user were to 
             * perform and 'undo' operation on the TextBox immediately after opening the Settings 
             * window. When this bug occurs, text inside the control is erased entirely, and the 
             * validation message "Curly braces are not permitted" is displayed in error. */

            this.SettingsDateFormatField.IsUndoEnabled = true;

#if OS_WINDOWS
            /* My implementation of window dragging will break for some reason when the user clicks
             * a ComboBox. Therefore, I have introduced logic to prevent dragging while the cursor
             * is hovering over any ComboBox identified here. */

            ComboBox[] comboBoxes = 
                [
                    FontFamilySelector,
                    FontWeightSelector
                ];

            ConfigureWindowDragBehaviour(SettingsWindowDragRoot, comboBoxes);
#endif
        });
    }

#if OS_WINDOWS
    private void ConfigureWindowDragBehaviour(
        Control? settingsWindowDragRoot, ComboBox[] comboBoxes)
    {
        if (settingsWindowDragRoot != null)
        {
            Observable.FromEventPattern<PointerEventArgs>(
                handler => settingsWindowDragRoot.PointerMoved += handler,
                handler => settingsWindowDragRoot.PointerMoved -= handler)
            // Does not need explicit disposal.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(eventPattern =>
                WindowDragHandle_PointerMoved(eventPattern.Sender, eventPattern.EventArgs));

            Observable.FromEventPattern<PointerPressedEventArgs>(
                handler => settingsWindowDragRoot.PointerPressed += handler,
                handler => settingsWindowDragRoot.PointerPressed -= handler)
            // Does not need explicit disposal.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(eventPattern =>
                WindowDragHandle_PointerPressed(eventPattern.Sender, eventPattern.EventArgs));

            Observable.FromEventPattern<PointerReleasedEventArgs>(
                handler => settingsWindowDragRoot.PointerReleased += handler,
                handler => settingsWindowDragRoot.PointerReleased -= handler)
            // Does not need explicit disposal.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(eventPattern =>
                WindowDragHandle_PointerReleased(
                    eventPattern.Sender, eventPattern.EventArgs));

            foreach (ComboBox currentComboBox in comboBoxes)
            {
                Observable.FromEventPattern<PointerEventArgs>(
                    handler => currentComboBox.PointerEntered += handler,
                    handler => currentComboBox.PointerEntered -= handler)
                // Does not need explicit disposal.
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => _isWindowDragPrevented = true);

                Observable.FromEventPattern<PointerEventArgs>(
                    handler => currentComboBox.PointerExited += handler,
                    handler => currentComboBox.PointerExited -= handler)
                // Does not need explicit disposal.
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => _isWindowDragPrevented = false);
            }
        }
    }

    private void WindowDragHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isWindowDragInEffect)
        {
            Point currentCursorPosition = e.GetPosition(this);
            Point cursorPositionDelta = currentCursorPosition - _cursorPositionAtWindowDragStart;

            Position = this.PointToScreen(cursorPositionDelta);
        }
    }

    private void WindowDragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isWindowDragPrevented)
        {
            _isWindowDragInEffect = true;
            _cursorPositionAtWindowDragStart = e.GetPosition(this);
        }
    }

    private void WindowDragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _isWindowDragInEffect = false;
#endif
}