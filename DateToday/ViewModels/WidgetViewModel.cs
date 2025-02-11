using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using DateToday.Models;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    [DataContract]
    public class WidgetViewModel : ViewModelBase
    {
        [IgnoreDataMember]
        private IWidgetView? _viewInterface;

        [IgnoreDataMember]
        private readonly WidgetModel _model = new();

        [IgnoreDataMember]
        private string _dateText = GetNewWidgetText();

        [IgnoreDataMember]
        private FontFamily _widgetFontFamily = FontManager.Current.DefaultFontFamily;

        [IgnoreDataMember]
        private PixelPoint _widgetPosition = PixelPoint.Origin;

        [IgnoreDataMember]
        private int _widgetFontSize = 75;

        [IgnoreDataMember]
        private string _lookupKeyWidgetFontWeight = FontWeight.Normal.ToString();

        [IgnoreDataMember]
        private FontWeight _widgetFontWeight = FontWeight.Normal;

        private static readonly Dictionary<string, FontWeight> _dictionaryFontWeightNames = new()
        {
            /* There are a number of ways to implement a drop-down FontWeight selector. I first
             * attempted simply to use a custom data binding converter such that the FontWeight enum
             * would be bound to, and each name converted to a string for presentation to the user.
             * The problem in this approach becomes apparent when one looks at the definition for
             * this enum, and notices that there are multiple names referring to the same values, 
             * such as ExtraLight and UltraLight, which both refer to the value 200. Therefore,
             * when Enum.GetValues() is called in order to populate a ComboBox with the names
             * corresponding to each FontWeight value, the computer simply chooses the first 
             * FontWeight name corresponding to each value, resulting in duplicate names being 
             * displayed. Furthermore, the user would be able to choose a FontWeight such as 
             * UltraLight, close and reopen the application, and then experience frustration when
             * the ExtraLight FontWeight was instead applied.
             * 
             * An alternative I considered would be to populate the ComboBox with a list of strings,
             * and use a custom data binding converter to parse the selected string as a FontWeight
             * enum, probably via Enum.Parse(). It is my opinion as a software developer that a 
             * string should never be cast to any other data type, and that doing so would 
             * constitute a code smell. But this is a conversation for another day.
             * 
             * Therefore, I settled on an approach in which a dictionary provides a mapping between 
             * a string key, and a FontWeight value. One of the benefits of this approach is that 
             * the displayed strings can be neatly formatted with spaces. */

            { "Thin", FontWeight.Thin },
            { "Extra Light", FontWeight.ExtraLight },
            { "Ultra Light", FontWeight.UltraLight },
            { "Light", FontWeight.Light },
            { "Semi Light", FontWeight.SemiLight },
            { "Normal", FontWeight.Normal },
            { "Regular", FontWeight.Regular },
            { "Medium", FontWeight.Medium },
            { "Demi Bold", FontWeight.DemiBold },
            { "Semi Bold", FontWeight.SemiBold },
            { "Bold", FontWeight.Bold },
            { "Extra Bold", FontWeight.ExtraBold },
            { "Ultra Bold", FontWeight.UltraBold },
            { "Black", FontWeight.Black },
            { "Heavy", FontWeight.Heavy },
            { "Solid", FontWeight.Solid },
            { "Extra Black", FontWeight.ExtraBlack },
            { "Ultra Black", FontWeight.UltraBlack }

            /* TODO: 
             *  I should probably move this dictionary somewhere else in order to better
             *  organise the codebase. Perhaps in AppSettings? */
        };

        public WidgetViewModel()
        {
            _model.ObservableNewMinuteEvent?
                .Subscribe
                (
                    _ => HandleNewMinuteEvent()
                );

            CommandReceiveNewSettings = ReactiveCommand.CreateFromTask(async () =>
                {
                    SettingsViewModel settingsViewModel = new(this);
                    await InteractionReceiveNewSettings.Handle(settingsViewModel);

                    RxApp.SuspensionHost.AppState = this;
                });

            CommandExitApplication = ReactiveCommand.Create(() =>
                {
                    if (Application.Current?.ApplicationLifetime is
                        IClassicDesktopStyleApplicationLifetime desktopApp)
                    {
                        desktopApp.Shutdown();
                    }
                });
        }

        public void AttachViewInterface(IWidgetView newViewInterface)
        {
            _viewInterface = newViewInterface;
            newViewInterface.WidgetPosition = _widgetPosition;
        }

        private static string GetNewWidgetText()
        {
            static string GetDaySuffix(int dayNumberInWeek)
            {
                return dayNumberInWeek switch
                {
                    1 or 21 or 31 => "st",
                    2 or 22 => "nd",
                    3 or 23 => "rd",
                    _ => "th",
                };
            }

            const string DATE_TEXT_FORMAT = "dddd', the 'd'{0} of 'MMMM";

            DateTime currentDateTime = DateTime.Now;
            Byte dayOfMonth = (byte)currentDateTime.Day;
            string daySuffix = GetDaySuffix(dayOfMonth);

            string dateTextBuffer = currentDateTime.ToString(DATE_TEXT_FORMAT);

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}");

            return string.Format(dateTextBuffer, daySuffix);
        }

        private void HandleNewMinuteEvent()
        {
            /* TODO: 
             * This behaviour should be altered such that the Model resets its tick generator 
             * interval independently of the View Model. I think that this is an example of business
             * logic, which should be encapsulated within the model. */

            _model.ResetTickGeneratorInterval();
            DateText = GetNewWidgetText();
        }

        [IgnoreDataMember]
        public IWidgetView? ViewInterface
        {
            set => _viewInterface = value;
        }

        [DataMember]
        public PixelPoint WidgetPosition
        {
            /* The private field _widgetPosition exists on this View Model as a reflection of its
             * View's Position property. This behaviour exists in order to facilitate ReactiveUI's
             * data persistence functionality: it is apparently very stupid, in that it refuses to 
             * save the current Position value when I access it via the IWidgetView interface. */

            get => _widgetPosition;
            set
            {
                _widgetPosition = value;

                /* By design, Avalonia does not permit the developer to bind to a property on its 
                 * View Model the Position of a given Window. 
                 *
                 * See:
                 *  https://github.com/AvaloniaUI/Avalonia/issues/3494
                 *
                 * Therefore, I assign to the WidgetWindow a Position via this setter method. */

                if (_viewInterface != null)
                {
                    _viewInterface.WidgetPosition = value;
                }
            }
        }

        [IgnoreDataMember]
        public FontFamily WidgetFontFamily
        {
            get => _widgetFontFamily;
            set => this.RaiseAndSetIfChanged(ref _widgetFontFamily, value);

            // TODO: Upon changing font, discard from memory the font selected previously.
        }

        [DataMember]
        public string WidgetFontFamilyName
        {
            /* This property exists because the ReactiveUI data persistence functionality doesn't
             * seem to be compatible with Avalonia's FontFamily class. */

            get => _widgetFontFamily.Name;
            set => WidgetFontFamily = value;
        }

        [DataMember]
        public int WidgetFontSize
        {
            get => _widgetFontSize;
            set 
            {
                this.RaiseAndSetIfChanged(ref _widgetFontSize, value);

                /* I have discovered a minor visual bug in Avalonia. I have configured the 
                 * WidgetWindow with a SizeToContent setting of WidthAndHeight. The bug occurs upon
                 * triggering a change in window size: the position of the window on-screen
                 * changes such that the window is lowered vertically relative to its previous 
                 * position.
                 * 
                 * TODO: Raise this bug with Avalonia.
                 * 
                 * Until this bug is resolved, I will manually reset the position of the 
                 * WidgetWindow upon reducing the WidgetFontSize. */

                WidgetPosition = WidgetPosition.WithY(_widgetPosition.Y);
            }
        }

        [DataMember]
        public string LookupKeyWidgetFontWeight
        {
            get => _lookupKeyWidgetFontWeight;
            set 
            {
                _lookupKeyWidgetFontWeight = value;

                bool isFontWeightValueFound = 
                    _dictionaryFontWeightNames.TryGetValue(
                        _lookupKeyWidgetFontWeight, out FontWeight newFontWeightValue
                    );

                if (isFontWeightValueFound)
                {
                    WidgetFontWeight = newFontWeightValue;
                }
            }
        }

        [IgnoreDataMember]
        public FontWeight WidgetFontWeight
        {
            get => _widgetFontWeight;
            set
            {
                this.RaiseAndSetIfChanged(ref _widgetFontWeight, value);
                WidgetPosition = WidgetPosition.WithY(_widgetPosition.Y);
            }
        }

        [IgnoreDataMember]
        public static Dictionary<string, FontWeight> DictionaryFontWeightNames => 
            _dictionaryFontWeightNames;

        [IgnoreDataMember]
        public string DateText 
        { 
            get => _dateText;
            set => this.RaiseAndSetIfChanged(ref _dateText, value);
        }

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand CommandReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand CommandExitApplication { get; }
    }
}
