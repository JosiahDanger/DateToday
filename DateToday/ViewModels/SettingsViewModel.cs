using Avalonia.Media;
using ReactiveUI;

namespace DateToday.ViewModels
{
    public class SettingsViewModel(WidgetViewModel widgetViewModel) : ViewModelBase
    {
        private readonly WidgetViewModel _widgetViewModel = widgetViewModel;

        public static string FontFamilyExampleWatermark
        {
            get => FontManager.Current.DefaultFontFamily.Name;
        }

        public int WidgetPositionX
        {
            get => _widgetViewModel.WidgetPosition.X;
            set => _widgetViewModel.WidgetPosition = _widgetViewModel.WidgetPosition.WithX(value);
        }

        public int WidgetPositionY
        {
            get => _widgetViewModel.WidgetPosition.Y;
            set => _widgetViewModel.WidgetPosition = _widgetViewModel.WidgetPosition.WithY(value);
        }

        public string WidgetFontFamilyName
        {
            get => _widgetViewModel.WidgetFontFamilyName;
            set => _widgetViewModel.WidgetFontFamilyName = value;
        }

        public ReactiveCommand<bool, bool> CommandCloseSettingsView { get; } =
            ReactiveCommand.Create<bool, bool>(dialogResult =>
            {
                /* This function will accept a dummy boolean value and pass it to the caller: the 
                 * WidgetViewModel instance. This behaviour is a vestige of cut functionality in 
                 * which the user would be able to manually save or revert changes to settings. I 
                 * will keep this here for now in case I want the SettingsWindow dialogue in the 
                 * future to return something meaningful. */

                return dialogResult;
            });
    }
}
