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
        PixelPoint Position { get; }
        void CloseView(object? dialogResult);
    }

    internal partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetView
    {
        public WidgetWindow()
        {
            InitializeComponent();

            this.WhenActivated(disposables => 
            {
                this.HandleActivation();

                ViewModel.WhenAnyValue(x => x.Position)
                         .ObserveOn(RxApp.MainThreadScheduler)
                         .BindTo(this, x => x.Position)
                         .DisposeWith(disposables);

                Observable.FromEventPattern<SizeChangedEventArgs>(
                    handler => SizeChanged += handler,
                    handler => SizeChanged -= handler
                )
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => OnViewSizeChanged(x.EventArgs))
                .DisposeWith(disposables);
            });
        }

        private void HandleActivation() 
        {
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowSettingsDialogAsync);
        }

        private void OnViewSizeChanged(SizeChangedEventArgs e)
        {
            static PixelPoint GetNewViewPositionMax(PixelSize monitorSize, Rect widgetBounds)
            {
                /* Calculate the maximum on-screen coordinate at which the widget will be fully 
                 * visible. */

                return
                    new(monitorSize.Width - (int)widgetBounds.Width,
                        monitorSize.Height - (int)widgetBounds.Height);
            }

            Screen? monitor = Screens.Primary;

            if (monitor != null)
            {
                PixelSize screenRealEstate = monitor.WorkingArea.Size;

                ViewModel!.PositionMax = GetNewViewPositionMax(screenRealEstate, Bounds);
                ConfineViewWithinScreenRealEstate(e, screenRealEstate);
            }
        }

        private void ConfineViewWithinScreenRealEstate(
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

        public void CloseView(object? dialogResult)
        {
            Close(dialogResult);
        }

        private async Task DoShowSettingsDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
            interaction.SetOutput(dialogResult);
        }
    }
}