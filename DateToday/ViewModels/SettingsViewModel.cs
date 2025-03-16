using Avalonia.Media;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;

namespace DateToday.ViewModels
{
    internal class SettingsViewModel(WidgetViewModel widgetViewModel) : ViewModelBase
    {
        private readonly WidgetViewModel _widgetViewModel = widgetViewModel;
        
        public int? WidgetPositionX
        {
            get => _widgetViewModel.Position.X;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.Position = 
                        _widgetViewModel.Position.WithX((int) value);
                }
            }
        }

        public int? WidgetPositionY
        {
            get => _widgetViewModel.Position.Y;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.Position =
                        _widgetViewModel.Position.WithY((int) value);
                }
            }
        }

        public FontFamily WidgetFontFamily
        {
            get => _widgetViewModel.FontFamily;
            set => _widgetViewModel.FontFamily = value.Name;
        }

        public int? WidgetFontSize
        {
            get => _widgetViewModel.FontSize;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.FontSize = (int) value;
                }
            }
        }

        public string WidgetFontWeightLookupKey
        {
            get => _widgetViewModel.FontWeightLookupKey;
            set 
            { 
                /* TODO: 
                 *  Why does the XAML binding sometimes try to set this value to null?
                 *  Do I need to fix this? */

                if (!string.IsNullOrEmpty(value))
                { 
                    _widgetViewModel.FontWeightLookupKey = value;
                }  
            }
        }

        public string WidgetDateFormat
        {
            get => _widgetViewModel.DateFormatUserInput;
            set => _widgetViewModel.DateFormatUserInput = value;
        }

        public static List<FontFamily> InstalledFontsList =>
            [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

        public Dictionary<string, FontWeight> FontWeightDictionary =>
            _widgetViewModel.FontWeightDictionary;

        public byte? WidgetOrdinalDaySuffixPosition
        {
            get => _widgetViewModel.OrdinalDaySuffixPosition;
            set => _widgetViewModel.OrdinalDaySuffixPosition = value;
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
