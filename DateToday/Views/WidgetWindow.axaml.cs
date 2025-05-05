using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
    internal interface IWidgetWindow
    {
        PixelPoint Position { get; }
        Color ThemedTextColour { get; }

        void Close(object? dialogResult);
    }

    internal partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetWindow
    {
        private const string RESOURCE_KEY_THEMED_TEXT_COLOUR = "SystemBaseHighColor";

        private readonly Color _themedTextColour;

        public WidgetWindow()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
            {
                // Make the previewer happy.
                return;
            }

            this.TryFindResource(
                RESOURCE_KEY_THEMED_TEXT_COLOUR, 
                ActualThemeVariant, 
                out var potentialThemedTextColourResource);

            if (potentialThemedTextColourResource is Color themedTextColourResource)
            {
                _themedTextColour = themedTextColourResource;
            }
            else
            {
                Debug.WriteLine("Failed to discern a thematically-appropriate text colour.");
                _themedTextColour = Colors.Black;
            }

            this.WhenActivated(disposables =>
            {
                this.HandleActivation();

                ViewModel.WhenAnyValue(x => x.WindowPosition)
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
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowSettingsDialogAsync);
        }

        private void OnWidgetSizeChanged(SizeChangedEventArgs e)
        {
            static PixelPoint GetNewWidgetPositionMax(PixelSize monitorSize, Rect widgetBounds)
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

                ViewModel!.WindowPositionMax = GetNewWidgetPositionMax(screenRealEstate, Bounds);
                ConfineWidgetWithinScreenRealEstate(e, screenRealEstate);
            }
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
                    int newPositionX =
                        Math.Max(ViewModel!.WindowPositionMax.X, PixelPoint.Origin.X);
                    ViewModel!.WindowPosition = Position.WithX(newPositionX);

                    Debug.WriteLine("Adjusted widget X position within monitor real estate.");
                }
            }

            if (hasHeightIncreased)
            {
                int widgetBottomEdgeY = this.PointToScreen(Bounds.BottomLeft).Y;
                int deltaY = widgetBottomEdgeY - screenRealEstate.Height;

                if (deltaY > 0)
                {
                    int newPositionY =
                        Math.Max(ViewModel!.WindowPositionMax.Y, PixelPoint.Origin.Y);
                    ViewModel!.WindowPosition = Position.WithY(newPositionY);

                    Debug.WriteLine("Adjusted widget Y position within monitor real estate.");
                }
            }
        }

        public Color ThemedTextColour => _themedTextColour;

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