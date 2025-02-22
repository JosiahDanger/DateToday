using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using DateToday.Models;
using DateToday.Structs;
using DateToday.Views;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private WidgetSettings _activeWidgetSettings;

        [IgnoreDataMember]
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        public WidgetViewModel()
        {
            static WidgetSettings GetDeserialisedDefaultWidgetSettings(string filepath)
            {
                string jsonBuffer = File.ReadAllText(filepath);
                return JsonConvert.DeserializeObject<WidgetSettings>(jsonBuffer);
            }

            static Dictionary<string, FontWeight> GetDeserialisedFontWeightDictionary(
                string filepath)
            {
                string jsonBuffer = File.ReadAllText(filepath);

                Dictionary<string, FontWeight>? dictionaryBuffer = 
                    JsonConvert.DeserializeObject<Dictionary<string, FontWeight>>(jsonBuffer);

                if (dictionaryBuffer != null)
                {
                    return dictionaryBuffer;
                }
                else
                {
                    // TODO: Error handling.
                    return [];
                }
            }

            _model.NewMinuteEventObservable?
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

            _activeWidgetSettings =
                GetDeserialisedDefaultWidgetSettings("DefaultWidgetSettings.json");

            _fontWeightDictionary = 
                GetDeserialisedFontWeightDictionary("FontWeightDictionary.json");
        }

        public void AttachViewInterface(IWidgetView newViewInterface)
        {
            _viewInterface = newViewInterface;
            newViewInterface.WidgetPosition = _activeWidgetSettings.WidgetPosition;
        }

        private void HandleNewMinuteEvent()
        {
            /* TODO: 
             * This behaviour should be altered such that the Model resets its tick generator 
             * interval independently of the View Model. I think that this is an example of business
             * logic, which should be encapsulated within the Model. */

            _model.ResetTickGeneratorInterval();
            DateText = GetNewWidgetText();
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

            // TODO: Make this configurable.
            const string DATE_TEXT_FORMAT = "dddd', the 'd'{0} of 'MMMM";

            DateTime currentDateTime = DateTime.Now;
            Byte dayOfMonth = (byte)currentDateTime.Day;
            string daySuffix = GetDaySuffix(dayOfMonth);

            string dateTextBuffer = currentDateTime.ToString(DATE_TEXT_FORMAT);

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}");

            return string.Format(dateTextBuffer, daySuffix);
        }

        private static FontWeight? AttemptFontWeightLookup(
            Dictionary<string, FontWeight>? fontWeightDictionary, string lookupKey)
        {
            if (fontWeightDictionary != null)
            {
                bool isFontWeightValueFound =
                    fontWeightDictionary.TryGetValue(lookupKey, out FontWeight newFontWeightValue);

                if (isFontWeightValueFound)
                {
                    return newFontWeightValue;
                }
            }

            return null;
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

            get => _activeWidgetSettings.WidgetPosition;
            set
            {
                _activeWidgetSettings.WidgetPosition = value;

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

            // TODO: Upon changing font, consider discarding from memory the font selected previously.
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
            get => _activeWidgetSettings.FontSize;
            set 
            {
                this.RaiseAndSetIfChanged(ref _activeWidgetSettings.FontSize, value);

                /* I have discovered a minor visual bug in Avalonia. I have configured the 
                 * WidgetWindow with a SizeToContent setting of WidthAndHeight. The bug occurs upon
                 * triggering a change in window size: the position of the window on-screen
                 * changes such that the window is lowered vertically relative to its previous 
                 * position.
                 * 
                 * TODO: Raise this bug with Avalonia. */
            }
        }

        [DataMember]
        public string WidgetFontWeightLookupKey
        {
            get => _activeWidgetSettings.FontWeightLookupKey;
            set 
            {
                _activeWidgetSettings.FontWeightLookupKey = value;

                FontWeight? fontWeightBuffer = 
                    AttemptFontWeightLookup(_fontWeightDictionary, value);

                if (fontWeightBuffer != null)
                {
                    WidgetFontWeight = fontWeightBuffer;
                }
            }
        }

        [IgnoreDataMember]
        public FontWeight? WidgetFontWeight
        {
            get
            {
                FontWeight? fontWeightBuffer =
                    AttemptFontWeightLookup(
                        _fontWeightDictionary, _activeWidgetSettings.FontWeightLookupKey
                    );

                if (fontWeightBuffer != null)
                {
                    return fontWeightBuffer;
                }

                // TODO: Error handling.
                return null;
            }

            set => this.RaisePropertyChanged();
        }

        [IgnoreDataMember]
        public Dictionary<string, FontWeight> FontWeightDictionary => _fontWeightDictionary;

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
