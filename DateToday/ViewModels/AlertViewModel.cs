using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Enums;
using ReactiveUI;
using System.Reactive;

namespace DateToday.ViewModels
{
    internal sealed class AlertViewModel : ReactiveObject
    {
        const string WINDOW_TITLE_INFORMATION = "Heads-up!";
        const string WINDOW_TITLE_WARNING = "Warning";
        const string WINDOW_TITLE_FATAL_ERROR = "OOPSIE WOOPSIE!!";

        const string BUTTON_CONTENT_INFORMATION = "Roger";
        const string BUTTON_CONTENT_WARNING = "Understood";
        const string BUTTON_CONTENT_FATAL_ERROR = "Exit";

        private const string RESOURCE_KEY_BACKGROUND_COLOUR_INFORMATION =
            "NotificationCardInformationBackgroundBrush";
        private const string RESOURCE_KEY_BACKGROUND_COLOUR_WARNING =
            "NotificationCardWarningBackgroundBrush";
        private const string RESOURCE_KEY_BACKGROUND_COLOUR_FATAL_ERROR =
            "NotificationCardErrorBackgroundBrush";

        private readonly string _windowTitle, _actionButtonContent, _alertMessage;

        private readonly IBrush _backgroundBrush;

        public ReactiveCommand<Unit, Unit> CloseAlert { get; } = 
            ReactiveCommand.Create(() => Unit.Default);

        public AlertViewModel(Window view, AlertType importance, string alertMessage)
        {
            _windowTitle = string.Empty;
            _actionButtonContent = string.Empty;

            _alertMessage = alertMessage;

            SolidColorBrush backgroundBrushFallback = new(Colors.White);

            switch (importance)
            {
                case AlertType.Information:

                    _windowTitle = WINDOW_TITLE_INFORMATION;

                    _actionButtonContent = BUTTON_CONTENT_INFORMATION;

                    _backgroundBrush =
                        Utilities.InitialiseThemedBrush(
                            view, RESOURCE_KEY_BACKGROUND_COLOUR_INFORMATION,
                            backgroundBrushFallback);

                    break;

                case AlertType.Warning:

                    _windowTitle = WINDOW_TITLE_WARNING;

                    _actionButtonContent = BUTTON_CONTENT_WARNING;

                    _backgroundBrush =
                        Utilities.InitialiseThemedBrush(
                            view, RESOURCE_KEY_BACKGROUND_COLOUR_WARNING,
                            backgroundBrushFallback);

                    break;

                default:

                    _windowTitle = WINDOW_TITLE_FATAL_ERROR;

                    _actionButtonContent = BUTTON_CONTENT_FATAL_ERROR;

                    _backgroundBrush =
                        Utilities.InitialiseThemedBrush(
                            view, RESOURCE_KEY_BACKGROUND_COLOUR_FATAL_ERROR,
                            backgroundBrushFallback);

                    break;
            }
        }

        public string WindowTitle => _windowTitle;

        public string AlertMessage => _alertMessage;

        public string ActionButtonContent => _actionButtonContent;

        public IBrush BackgroundBrush => _backgroundBrush;
    }
}
