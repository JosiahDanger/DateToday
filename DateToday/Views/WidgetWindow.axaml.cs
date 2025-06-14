using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using DateToday.Enums;
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
                ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowSettingsDialogAsync);

                IObservable<PixelPoint> newPositionFromSizeChangeObservable =
                    Observable.FromEventPattern<SizeChangedEventArgs>(
                        handler => SizeChanged += handler,
                        handler => SizeChanged -= handler
                    )
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(_ => 
                            ViewModel.AnchoredCornerScaledPositionMax =
                                CalculateScaledPositionMax(ClientSize, DesktopWorkingAreaOrNull))
                    .Select(_ => 
                                CalculateOverallScaledPosition(
                                    ViewModel.AnchoredCorner, 
                                    ViewModel.AnchoredCornerScaledPosition, 
                                    ClientSize, DesktopWorkingAreaOrNull))
                    .Select(newScaledPosition => 
                                ClampScaledPosition(
                                    newScaledPosition, ViewModel.AnchoredCornerScaledPositionMax))
                    .Select(clampedScaledPosition => 
                                PixelPoint.FromPoint(clampedScaledPosition, DesktopScaling));

                IObservable<PixelPoint> newPositionFromAnchoredCornerChangeObservable =
                    ViewModel.WhenAnyValue(wvm => wvm.AnchoredCorner,
                                           wvm => wvm.AnchoredCornerScaledPosition)
                             .ObserveOn(RxApp.MainThreadScheduler)
                             .Select(args =>
                                        CalculateOverallScaledPosition(
                                            args.Item1, args.Item2,
                                            ClientSize, DesktopWorkingAreaOrNull))
                             .Select(scaledPosition =>
                                        PixelPoint.FromPoint(scaledPosition, DesktopScaling));

                Observable.FromEventPattern(
                    handler => LayoutUpdated += handler,
                    handler => LayoutUpdated -= handler
                )
                // Does not need explicit disposal.
                .ObserveOn(RxApp.MainThreadScheduler)
                .Take(1) // Do this only once.
                .Subscribe(_ => 
                {
                    ViewModel.AnchoredCornerScaledPositionMax =
                        /* A new SizeChanged event will not occur as a direct result of binding its 
                         * Observable stream. Therefore, this property on the view model must be 
                         * initialised here. */
                        CalculateScaledPositionMax(ClientSize, DesktopWorkingAreaOrNull);

                    newPositionFromSizeChangeObservable
                    .BindTo(this, widgetWindow => widgetWindow.Position);

                    newPositionFromAnchoredCornerChangeObservable
                    .BindTo(this, widgetWindow => widgetWindow.Position)
                    .DisposeWith(disposables);
                });
            });
        }

        private static Point CalculateScaledPositionMax(
            Size widgetSize, Size? desktopWorkingAreaOrNull)
        {
            /* Calculate the maximum on-screen coordinate at which the widget will be fully visible, 
             * taking into account the operating system desktop scaling factors reflected in the 
             * input Size structs. */

            if (desktopWorkingAreaOrNull is Size desktopWorkingArea)
            {
                return
                    new(desktopWorkingArea.Width - widgetSize.Width,
                        desktopWorkingArea.Height - widgetSize.Height);
            }

            return new(double.PositiveInfinity, double.PositiveInfinity);
        }

        private static Point ClampScaledPosition(Point currentPosition, Point maxPosition)
        {
            /* When the widget is automatically resized to fit its contents, the application will 
             * henceforth adjust the widget's position according to which corner is anchored. In 
             * doing so, the widget can move off-screen. The purpose of this function is to limit 
             * the overall position of the widget such that it is enclosed entirely within the 
             * bounds of the desktop working area. */

            double newPositionX = Math.Clamp(currentPosition.X, 0, maxPosition.X);
            double newPositionY = Math.Clamp(currentPosition.Y, 0, maxPosition.Y);

            return new(newPositionX, newPositionY);
        }

        private static Point CalculateOverallScaledPosition(
            WindowVertexIdentifier anchoredCorner, Point anchoredCornerScaledPosition,
            Size widgetSize, Size? desktopWorkingAreaOrNull)
        {
            /* Given the scaled position of a specified widget corner, calculate the scaled position 
             * of the top-left corner. If the top-left corner is anchored, this function doesn't 
             * need to do anything. */

            if (desktopWorkingAreaOrNull is Size desktopWorkingArea)
            {
                double newScaledPositionX;
                double newScaledPositionY;

                switch (anchoredCorner)
                {
                    case WindowVertexIdentifier.TopRight:

                        newScaledPositionX =
                            desktopWorkingArea.Width - widgetSize.Width -
                            anchoredCornerScaledPosition.X;

                        return new(newScaledPositionX, anchoredCornerScaledPosition.Y);

                    case WindowVertexIdentifier.BottomLeft:

                        newScaledPositionY =
                            desktopWorkingArea.Height - widgetSize.Height -
                            anchoredCornerScaledPosition.Y;

                        return new(anchoredCornerScaledPosition.X, newScaledPositionY);

                    case WindowVertexIdentifier.BottomRight:

                        newScaledPositionX =
                            desktopWorkingArea.Width - widgetSize.Width -
                            anchoredCornerScaledPosition.X;

                        newScaledPositionY =
                            desktopWorkingArea.Height - widgetSize.Height -
                            anchoredCornerScaledPosition.Y;

                        return new(newScaledPositionX, newScaledPositionY);
                }
            }

            return anchoredCornerScaledPosition;
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

        private async Task DoShowSettingsDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
            interaction.SetOutput(dialogResult);
        }

        private Size? DesktopWorkingAreaOrNull => 
            Screens.Primary?.WorkingArea.Size.ToSize(DesktopScaling);

        public Color ThemedTextColour => _themedTextColour;

        public Color ThemedTextShadowColour => _themedTextShadowColour;
    }
}