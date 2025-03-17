using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DateToday.ViewModels
{
    internal class SettingsViewModel : ViewModelBase, IActivatableViewModel
    {
        private readonly WidgetViewModel _widgetViewModel;

        public SettingsViewModel(WidgetViewModel widgetViewModel)
        {
            _widgetViewModel = widgetViewModel;

            this.WhenActivated(disposables => 
            {
                _widgetViewModel.WhenAnyValue(x => x.PositionOAPH)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(x => x.X)
                                .ToProperty(this, nameof(WidgetPositionX))
                                .DisposeWith(disposables);

                _widgetViewModel.WhenAnyValue(x => x.PositionOAPH)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(x => x.Y)
                                .ToProperty(this, nameof(WidgetPositionY))
                                .DisposeWith(disposables);

                _widgetViewModel.WhenAnyValue(x => x.PositionMax)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .ToProperty(this, nameof(WidgetPositionMax))
                                .DisposeWith(disposables);
            });
        }

        public int WidgetPositionX
        {
            get => _widgetViewModel.PositionOAPH.X;
            set
            {
                _widgetViewModel.Position = 
                    _widgetViewModel.PositionOAPH.WithX(value);
            }
        }

        public int WidgetPositionY
        {
            get => _widgetViewModel.PositionOAPH.Y;
            set
            {
                _widgetViewModel.Position =
                    _widgetViewModel.PositionOAPH.WithY(value);
            }
        }

        public PixelPoint WidgetPositionMax => _widgetViewModel.PositionMax;

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

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

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
