using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Models;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private readonly IWidgetView _viewInterface;

        [IgnoreDataMember]
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        [IgnoreDataMember]
        private readonly WidgetModel _model;

        [IgnoreDataMember]
        private string _dateText;

        [IgnoreDataMember]
        private int? _widgetFontWeightValue;

        [IgnoreDataMember]
        private FontFamily _widgetFontFamily;

        [IgnoreDataMember]
        private PixelPoint _widgetPosition;

        [IgnoreDataMember]
        private readonly ObservableAsPropertyHelper<PixelPoint> _positionOAPH;

        [IgnoreDataMember]
        private int _widgetFontSize;

        [IgnoreDataMember]
        private string _widgetFontWeightLookupKey;

        [IgnoreDataMember]
        private string _widgetDateFormat;

        [IgnoreDataMember]
        private byte? _widgetOrdinalDaySuffixPosition;

        public WidgetViewModel(
            IWidgetView viewInterface, 
            WidgetModel model,
            Dictionary<string, FontWeight> fontWeightDictionary,
            WidgetConfiguration restoredSettings)
        {
            _dateText = string.Empty;

            _viewInterface = viewInterface;
            _model = model;
            _fontWeightDictionary = fontWeightDictionary;

            // Depends on operating system. Default is empty.
            _widgetFontFamily = 
                restoredSettings.FontFamilyName ?? FontManager.Current.DefaultFontFamily;

            _widgetPosition = restoredSettings.Position;
            _widgetFontSize = restoredSettings.FontSize;
            _widgetFontWeightLookupKey = restoredSettings.FontWeightLookupKey;
            _widgetFontWeightValue = 
                AttemptFontWeightLookup(_fontWeightDictionary, FontWeightLookupKey);
            _widgetDateFormat = restoredSettings.DateFormat;
            _widgetOrdinalDaySuffixPosition = restoredSettings.OrdinalDaySuffixPosition;

            _positionOAPH = this
                .WhenAnyValue(x => x.Position)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(PositionOAPH));

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
        }

        private void HandleActivation()
        {
            RefreshDateText();
        }

        public void Dispose()
        {
            _model.Dispose();
            _positionOAPH.Dispose();

            Debug.WriteLine("Disposed of View Model");
            GC.SuppressFinalize(this);
        }

        private void RefreshDateText()
        {
            DateText = GetNewDateText(DateFormat, OrdinalDaySuffixPosition);   
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
                    return (int)newFontWeightValue;
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
        public PixelPoint Position
        {
            get => _widgetPosition;
            set => this.RaiseAndSetIfChanged(ref _widgetPosition, value);
        }

        [DataMember]
        private string FontFamilyName
        {
            /* This property exists because the ReactiveUI data persistence functionality doesn't 
             * support Avalonia's FontFamily data type. */

            get => _widgetFontFamily.Name;
        }

        [IgnoreDataMember]
        public FontFamily FontFamily
        {
            get => _widgetFontFamily;
            set => this.RaiseAndSetIfChanged(ref _widgetFontFamily, value);
        }

        [DataMember]
        public int FontSize
        {
            get => _widgetFontSize;
            set => this.RaiseAndSetIfChanged(ref _widgetFontSize, value);
        }

        [DataMember]
        public string FontWeightLookupKey
        {
            get => _widgetFontWeightLookupKey;
            set
            {
                this.RaiseAndSetIfChanged(ref _widgetFontWeightLookupKey, value);

                int? newFontWeightValue = AttemptFontWeightLookup(_fontWeightDictionary, value);

                if (newFontWeightValue != null)
                {
                    FontWeight = newFontWeightValue;
                }
            }
        }

        [IgnoreDataMember]
        public int? FontWeight
        {
            get => _widgetFontWeightValue;
            set => this.RaiseAndSetIfChanged(ref _widgetFontWeightValue, value);
        }

        [IgnoreDataMember]
        public string DateFormatUserInput
        {
            get => _widgetDateFormat;
            set
            {
                string newDateText =
                    AttemptToGetNewDateTextFromDateFormatUserInput(value, _widgetOrdinalDaySuffixPosition);

                DateFormat = value;
                DateText = newDateText;
            }
        }

        [DataMember]
        private string DateFormat
        {
            get => _widgetDateFormat;
            set => _widgetDateFormat = value;
        }

        [DataMember]
        public byte? OrdinalDaySuffixPosition
        {
            get => _widgetOrdinalDaySuffixPosition;
            set
            {
                if (value > _widgetDateFormat.Length)
                {
                    throw new DataValidationException("Exceeds format length");
                }

                _widgetOrdinalDaySuffixPosition = value;
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
        public PixelPoint PositionOAPH => _positionOAPH.Value;

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand CommandReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand CommandExitApplication { get; }
    }
}
