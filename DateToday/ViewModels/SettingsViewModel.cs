using Avalonia.Media;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;

namespace DateToday.ViewModels
{
    public class SettingsViewModel(WidgetViewModel widgetViewModel) : ViewModelBase
    {
        private readonly WidgetViewModel _widgetViewModel = widgetViewModel;
        
        public int? WidgetPositionX
        {
            get => _widgetViewModel.WidgetPosition.X;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.WidgetPosition = 
                        _widgetViewModel.WidgetPosition.WithX((int) value);
                }
            }
        }

        public int? WidgetPositionY
        {
            get => _widgetViewModel.WidgetPosition.Y;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.WidgetPosition =
                        _widgetViewModel.WidgetPosition.WithY((int) value);
                }
            }
        }

        public FontFamily WidgetFontFamily
        {
            get => _widgetViewModel.WidgetFontFamilyName;
            set => _widgetViewModel.WidgetFontFamilyName = value.Name;
        }

        public int? WidgetFontSize
        {
            get => _widgetViewModel.WidgetFontSize;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.WidgetFontSize = (int) value;
                }
            }
        }

        public string LookupKeyWidgetFontWeight
        {
            get => _widgetViewModel.LookupKeyWidgetFontWeight;
            set => _widgetViewModel.LookupKeyWidgetFontWeight = value;
        }

        public static List<FontFamily> ListInstalledFonts =>
            [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

        public static List<string> ListLookupKeysFontWeight =>
            [.. WidgetViewModel.DictionaryFontWeightNames.Keys];

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
