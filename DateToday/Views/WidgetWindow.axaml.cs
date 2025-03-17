using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DateToday.Views
{
    internal interface IWidgetView
    {
        PixelPoint WidgetPosition { get; }
        void CloseWidget(object? dialogResult);
    }

    internal partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetView
    {
        public WidgetWindow()
        {
            if (Design.IsDesignMode)
            {
                // Make the previewer happy.
                return;
            };

            InitializeComponent();

            this.WhenActivated(disposables => 
            {
                this.HandleActivation();

                Disposable
                    .Create(() => this.HandleDeactivation())
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.PositionOAPH)
                         .ObserveOn(RxApp.MainThreadScheduler)
                         .BindTo(this, x => x.Position)
                         .DisposeWith(disposables);

                Observable.FromEventPattern<SizeChangedEventArgs>(
                    handler => SizeChanged += handler,
                    handler => SizeChanged -= handler
                )
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => OnWidgetSizeChanged(x.EventArgs))
                .DisposeWith(disposables);
            });
        }

        private void HandleActivation() 
        {
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowDialogAsync);
        }

        private void HandleDeactivation()
        {
            ViewModel!.Dispose();
        }

        private void OnWidgetSizeChanged(SizeChangedEventArgs e)
        {
            Screen? monitor = Screens.Primary;

            if (monitor != null)
            {
                PixelSize screenRealEstate = monitor.WorkingArea.Size;

                ViewModel!.PositionMax = GetNewPositionMax(screenRealEstate, Bounds);
                ConfineWidgetWithinScreenRealEstate(e, screenRealEstate);
            }
        }

        private static PixelPoint GetNewPositionMax(PixelSize monitorSize, Rect widgetBounds)
        {
            // Calculate the maximum on-screen coordinate at which the widget will be fully visible.

            return 
                new(monitorSize.Width - (int)widgetBounds.Width, 
                    monitorSize.Height - (int)widgetBounds.Height);
        }

        private void ConfineWidgetWithinScreenRealEstate(
            SizeChangedEventArgs e, PixelSize screenRealEstate)
        {
            bool hasWidthIncreased = e.NewSize.Width > e.PreviousSize.Width;
            bool hasHeightIncreased = e.NewSize.Height > e.PreviousSize.Height;

            if (hasWidthIncreased)
            {
                int widgetRightEdgeX = this.PointToScreen(Bounds.TopRight).X;
                int deltaX = widgetRightEdgeX - screenRealEstate.Width;

                if (deltaX > 0)
                {
                    int newPositionX = Math.Max(ViewModel!.PositionMax.X, PixelPoint.Origin.X);
                    ViewModel!.Position = Position.WithX(newPositionX);

                    Debug.WriteLine("Adjusted widget X position within monitor real estate.");
                }
            }

            if (hasHeightIncreased)
            {
                int widgetBottomEdgeY = this.PointToScreen(Bounds.BottomLeft).Y;
                int deltaY = widgetBottomEdgeY - screenRealEstate.Height;

                if (deltaY > 0)
                {
                    int newPositionY = Math.Max(ViewModel!.PositionMax.Y, PixelPoint.Origin.Y);
                    ViewModel!.Position = Position.WithY(newPositionY);

                    Debug.WriteLine("Adjusted widget Y position within monitor real estate.");
                }
            }
        }

        public PixelPoint WidgetPosition => Position;

        public void CloseWidget(object? dialogResult)
        {
            Close(dialogResult);
        }

        private async Task DoShowDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
            interaction.SetOutput(dialogResult);
        }
    }
}