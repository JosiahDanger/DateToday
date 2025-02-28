using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
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
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    [DataContract]
    public class WidgetViewModel : ViewModelBase
    {
        [IgnoreDataMember]
        private IWidgetView? _viewInterface = null;

        [IgnoreDataMember]
        private readonly WidgetModel _model = new();

        [IgnoreDataMember]
        private bool _isViewModelInitialised = false;

        [IgnoreDataMember]
        private string _dateText = string.Empty;

        [IgnoreDataMember]
        private FontFamily _widgetFontFamily = FontManager.Current.DefaultFontFamily;

        [IgnoreDataMember]
        private int? _widgetFontWeightValue = null;

        [IgnoreDataMember]
        private WidgetConfiguration _activeWidgetConfiguration;

        [IgnoreDataMember]
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        public WidgetViewModel()
        {
            static WidgetConfiguration GetDeserialisedDefaultWidgetConfiguration(string filepath)
            {
                string jsonBuffer = File.ReadAllText(filepath);
                return JsonConvert.DeserializeObject<WidgetConfiguration>(jsonBuffer);
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
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => RefreshDateText());

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

            _activeWidgetConfiguration =
                GetDeserialisedDefaultWidgetConfiguration("DefaultWidgetConfiguration.json");

            _fontWeightDictionary = 
                GetDeserialisedFontWeightDictionary("FontWeightDictionary.json");

            _widgetFontWeightValue =
                AttemptFontWeightLookup(
                    _fontWeightDictionary, _activeWidgetConfiguration.FontWeightLookupKey);
        }

        public void AttachViewInterface(IWidgetView newViewInterface)
        {
            _viewInterface = newViewInterface;
            newViewInterface.WidgetPosition = WidgetPosition;
        }

        public void OnViewModelInitialised()
        {
            _isViewModelInitialised = true;
            RefreshDateText();
        }

        private void RefreshDateText()
        {
            if (_isViewModelInitialised)
            {
                DateText = GetNewDateText(WidgetDateFormat, OrdinalDaySuffixPosition);
            }
        }

        private static string GetNewDateText(string dateFormat, byte? ordinalDaySuffixPosition)
        {
            static string GetOrdinalDaySuffix(int dayNumberInWeek)
            {
                return dayNumberInWeek switch
                {
                    1 or 21 or 31 => "st",
                    2 or 22 => "nd",
                    3 or 23 => "rd",
                    _ => "th",
                };
            }

            DateTime currentDateTime = DateTime.Now;
            Debug.WriteLine($"Refreshed widget text at {currentDateTime}");

            if (ordinalDaySuffixPosition != null)
            {
                Byte dayOfMonth = (byte)currentDateTime.Day;
                string ordinalDaySuffix = GetOrdinalDaySuffix(dayOfMonth);

                string finalDateFormat = 
                    dateFormat.Insert((int) ordinalDaySuffixPosition, "{0}");

                return
                    string.Format(
                        currentDateTime.ToString(finalDateFormat), 
                        ordinalDaySuffix
                    );
            }

            return currentDateTime.ToString(dateFormat);
        }

        private static int? AttemptFontWeightLookup(
            Dictionary<string, FontWeight>? fontWeightDictionary, string lookupKey)
        {
            if (fontWeightDictionary != null)
            {
                bool isFontWeightValueFound =
                    fontWeightDictionary.TryGetValue(lookupKey, out FontWeight newFontWeightValue);

                if (isFontWeightValueFound)
                {
                    return (int) newFontWeightValue;
                }
            }

            return null;
        }

        private static string AttemptToGetNewDateTextFromDateFormatUserInput(
            string newDateFormat, byte? ordinalDaySuffixPosition)
        {
            // TODO: Put all validation message strings in a new JSON file.

            if (string.IsNullOrEmpty(newDateFormat))
            {
                throw new DataValidationException("Please enter a date format");
            }

            if (ordinalDaySuffixPosition != null && ordinalDaySuffixPosition > newDateFormat.Length)
            {
                throw new DataValidationException(
                    "Ordinal day suffifx position exceeds length of new date format");
            }

            string[] curlyBraces = ["{", "}"];

            if (curlyBraces.Any(newDateFormat.Contains))
            {
                throw new DataValidationException("Curly braces are not permitted");
            }

            try
            {
                return GetNewDateText(newDateFormat, ordinalDaySuffixPosition);
            }
            catch (System.FormatException)
            {
                throw new DataValidationException("Invalid date format");
            }
        }

        [IgnoreDataMember]
        public IWidgetView? ViewInterface { set => _viewInterface = value; }

        [DataMember]
        public PixelPoint WidgetPosition
        {
            /* The private field _widgetPosition exists on this View Model as a reflection of its
             * View's Position property. This behaviour exists in order to facilitate ReactiveUI's
             * data persistence functionality: it is apparently very stupid, in that it refuses to 
             * save the current Position value when I access it via the IWidgetView interface. */

            get => _activeWidgetConfiguration.Position;
            set
            {
                _activeWidgetConfiguration.Position = value;

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

        [DataMember]
        private string WidgetFontFamilyName
        {
            /* This property exists because the ReactiveUI data persistence functionality doesn't 
             * support Avalonia's FontFamily data type. */

            get => _widgetFontFamily.Name;
            set => WidgetFontFamily = value;
        }

        [IgnoreDataMember]
        public FontFamily WidgetFontFamily
        {
            get => _widgetFontFamily;
            set => this.RaiseAndSetIfChanged(ref _widgetFontFamily, value);
        }

        [DataMember]
        public int WidgetFontSize
        {
            get => _activeWidgetConfiguration.FontSize;
            set 
            {
                this.RaiseAndSetIfChanged(ref _activeWidgetConfiguration.FontSize, value);

                /* I have discovered a minor visual bug in Avalonia. I have configured the 
                 * WidgetWindow with a SizeToContent setting of WidthAndHeight. The bug occurs upon
                 * triggering a change in window size: the on-screen position of the window 
                 * changes such that the window is lowered vertically relative to its previous 
                 * position.
                 * 
                 * TODO: Raise this bug with Avalonia. */
            }
        }

        [DataMember]
        public string WidgetFontWeightLookupKey
        {
            get => _activeWidgetConfiguration.FontWeightLookupKey;
            set 
            {
                this.RaiseAndSetIfChanged(
                    ref _activeWidgetConfiguration.FontWeightLookupKey, value);

                int? newFontWeightValue = AttemptFontWeightLookup(_fontWeightDictionary, value);

                if (newFontWeightValue != null)
                {
                    WidgetFontWeight = newFontWeightValue;
                }
            }
        }

        [IgnoreDataMember]
        public int? WidgetFontWeight
        {
            get => _widgetFontWeightValue;
            set => this.RaiseAndSetIfChanged(ref _widgetFontWeightValue, value);
        }

        [IgnoreDataMember]
        public string WidgetDateFormatUserInput
        {
            get => WidgetDateFormat;
            set
            {
                string newDateText = 
                    AttemptToGetNewDateTextFromDateFormatUserInput(value, OrdinalDaySuffixPosition);
                
                WidgetDateFormat = value;
                DateText = newDateText;
            }
        }

        [DataMember]
        private string WidgetDateFormat
        {
            get => _activeWidgetConfiguration.DateFormat;
            set => _activeWidgetConfiguration.DateFormat = value;
        }
        
        [DataMember]
        public byte? OrdinalDaySuffixPosition
        {
            get => _activeWidgetConfiguration.OrdinalDaySuffixPosition;
            set
            {
                if (_isViewModelInitialised && value > WidgetDateFormatUserInput.Length)
                {
                    throw new DataValidationException("Exceeds format length");
                }

                _activeWidgetConfiguration.OrdinalDaySuffixPosition = value;
                RefreshDateText();
            }
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
