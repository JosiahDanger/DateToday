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
        Color ThemedTextShadowColour { get; }

        void Close(object? dialogResult);
    }

    internal sealed partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetWindow
    {
        private const string RESOURCE_KEY_THEMED_TEXT_COLOUR = "SystemBaseHighColor";
        private const string RESOURCE_KEY_THEMED_TEXT_SHADOW_COLOUR = "SystemRegionColor";

        private readonly Color _themedTextColour, _themedTextShadowColour;

        public WidgetWindow()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
            {
                // Make the previewer happy.
                return;
            }

            _themedTextColour = 
                InitialiseThemedColour(RESOURCE_KEY_THEMED_TEXT_COLOUR, Colors.Black);

            _themedTextShadowColour = 
                InitialiseThemedColour(RESOURCE_KEY_THEMED_TEXT_SHADOW_COLOUR, Colors.White);

            this.WhenActivated(disposables =>
            {
                this.HandleActivation();

                ViewModel.WhenAnyValue(widgetViewModel => widgetViewModel.WidgetPosition)
                         .ObserveOn(RxApp.MainThreadScheduler)
                         .Select(point => PixelPoint.FromPoint(point, this.DesktopScaling))
                         .BindTo(this, widgetWindow => widgetWindow.Position)
                         .DisposeWith(disposables);

                Observable.FromEventPattern<SizeChangedEventArgs>(
                    handler => SizeChanged += handler,
                    handler => SizeChanged -= handler
                )
                // Does not need explicit disposal.
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => WidgetWindow_OnSizeChanged(x.EventArgs));
            });
        }

        private void HandleActivation()
        {
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowSettingsDialogAsync);
        }

        private void WidgetWindow_OnSizeChanged(SizeChangedEventArgs e)
        {
            static Point GetNewWidgetPositionMax(Size monitorSize, Rect widgetBounds)
            {
                /* Calculate the maximum on-screen coordinate at which the widget will be fully 
                 * visible. */

                return
                    new(monitorSize.Width - widgetBounds.Width, 
                        monitorSize.Height - widgetBounds.Height);
            }

            Screen? monitor = Screens.Primary;

            if (monitor != null)
            {
                Size screenRealEstate = monitor.WorkingArea.Size.ToSize(DesktopScaling);

                ViewModel!.WidgetPositionMax = GetNewWidgetPositionMax(screenRealEstate, Bounds);
                ConfineWidgetWithinScreenRealEstate(e, screenRealEstate);
            }
        }

        private void ConfineWidgetWithinScreenRealEstate(
            SizeChangedEventArgs e, Size screenRealEstate)
        {
            /* Avalonia's Point Struct takes into account operating system display scaling, whereas 
             * PixelPoint does not. 
             * 
             * 'widgetClientPosition' will be used to store the widget's position in a 
             * "device-independent" Point value. */

            Point widgetClientPosition;
            
            bool hasWidthIncreased = e.NewSize.Width > e.PreviousSize.Width;
            bool hasHeightIncreased = e.NewSize.Height > e.PreviousSize.Height;

            if (hasWidthIncreased)
            {
                widgetClientPosition = Position.ToPoint(DesktopScaling);

                double widgetRightEdgeX = widgetClientPosition.X + e.NewSize.Width;
                double deltaX = widgetRightEdgeX - screenRealEstate.Width;

                if (deltaX > 0)
                {
                    double newPositionX = Math.Max(ViewModel!.WidgetPositionMax.X, 0);
                    ViewModel!.WidgetPosition = widgetClientPosition.WithX(newPositionX);

                    Debug.WriteLine("Adjusted widget X position within monitor real estate.");
                }
            }

            if (hasHeightIncreased)
            {
                widgetClientPosition = Position.ToPoint(DesktopScaling);

                double widgetBottomEdgeY = widgetClientPosition.Y + e.NewSize.Height;
                double deltaY = widgetBottomEdgeY - screenRealEstate.Height;

                if (deltaY > 0)
                {
                    double newPositionY = Math.Max(ViewModel!.WidgetPositionMax.Y, 0);
                    ViewModel!.WidgetPosition = widgetClientPosition.WithY(newPositionY);

                    Debug.WriteLine("Adjusted widget Y position within monitor real estate.");
                }
            }
        }

        private Color InitialiseThemedColour(string resourceKey, Color fallback)
        {
            this.TryFindResource(
                resourceKey,
                ActualThemeVariant,
                out var themedColourResourceOrNull);

            if (themedColourResourceOrNull is Color themedColourResource)
            {
                return themedColourResource;
            }
            else
            {
                Debug.WriteLine(
                    $"Failed to discern thematically-appropriate colour associated with key: " +
                    $"'{resourceKey}'. Using {fallback} instead.");

                return fallback;
            }
        }

        public Color ThemedTextColour => _themedTextColour;

        public Color ThemedTextShadowColour => _themedTextShadowColour;

        private async Task DoShowSettingsDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
            interaction.SetOutput(dialogResult);
        }
    }
}