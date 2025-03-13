using Avalonia;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    [DataContract]
    internal class WidgetViewModel : ViewModelBase, IActivatableViewModel, IDisposable
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

            this.WhenActivated(disposables =>
            {
                this.HandleActivation();

                _model.NewMinuteEventObservable?
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => RefreshDateText())
                      .DisposeWith(disposables);
            });

            CommandReceiveNewSettings = ReactiveCommand.CreateFromTask(async () =>
                {
                    SettingsViewModel settingsViewModel = new(this);
                    await InteractionReceiveNewSettings.Handle(settingsViewModel);

                    RxApp.SuspensionHost.AppState = this;
                });

            CommandExitApplication = ReactiveCommand.Create(() =>
                {
                    _viewInterface?.CloseWidget(0);
                });

            _activeWidgetConfiguration =
                GetDeserialisedDefaultWidgetConfiguration("DefaultWidgetConfiguration.json");

            _fontWeightDictionary = 
                GetDeserialisedFontWeightDictionary("FontWeightDictionary.json");

            _widgetFontWeightValue =
                AttemptFontWeightLookup(
                    _fontWeightDictionary, _activeWidgetConfiguration.FontWeightLookupKey);
        }

        private void HandleActivation()
        {
            _isViewModelInitialised = true;
            RefreshDateText();
        }

        public void Dispose()
        {
            _model.Dispose();
            Debug.WriteLine("Disposed of View Model");
            GC.SuppressFinalize(this);
        }

        internal void AttachViewInterface(IWidgetView newViewInterface)
        {
            _viewInterface = newViewInterface;

            if (newViewInterface != null)
            {
                newViewInterface.WidgetPosition = WidgetPosition;
            }
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

            CultureInfo operatingSystemCulture = CultureInfo.CurrentCulture;
            DateTime currentDateTime = DateTime.Now;
            
            string formattedDateOutput;

            if (ordinalDaySuffixPosition != null)
            {
                Byte dayOfMonth = (byte)currentDateTime.Day;
                string ordinalDaySuffix = GetOrdinalDaySuffix(dayOfMonth);

                /* This code makes use of .NET composite formatting.
                 * 
                 * See the following Microsoft Learn article:
                 * https://learn.microsoft.com/dotnet/standard/base-types/composite-formatting */

                string dateFormatIncludingFormatItem =
                    dateFormat.Insert((int)ordinalDaySuffixPosition, "{0}");

                string formattedDateIncludingFormatItem = 
                    currentDateTime.ToString(dateFormatIncludingFormatItem, operatingSystemCulture);

                formattedDateOutput = 
                    string.Format(
                        operatingSystemCulture, formattedDateIncludingFormatItem, ordinalDaySuffix);
            }
            else
            {
                formattedDateOutput = currentDateTime.ToString(dateFormat, operatingSystemCulture);
            }

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}");
            return formattedDateOutput;
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

            if (string.IsNullOrWhiteSpace(newDateFormat))
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

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand CommandReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand CommandExitApplication { get; }
    }
}
