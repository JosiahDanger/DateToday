using Avalonia;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Models;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    [DataContract]
    internal class WidgetViewModel : ViewModelBase, IActivatableViewModel, IDisposable
    {
        // TODO: Address style inconsistencies regarding the substring 'widget' in field names.

        [IgnoreDataMember]
        private readonly IWidgetView _viewInterface;

        [IgnoreDataMember]
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        [IgnoreDataMember]
        private readonly WidgetModel _model;

        [IgnoreDataMember]
        private string _dateText, _dateFormat, _dateFormatUserInput;

        [IgnoreDataMember]
        private int? _widgetFontWeightValue;

        [IgnoreDataMember]
        private FontFamily _widgetFontFamily;

        [IgnoreDataMember]
        private PixelPoint _widgetPosition;

        [IgnoreDataMember]
        private readonly ObservableAsPropertyHelper<PixelPoint> _positionOAPH;

        [IgnoreDataMember]
        private PixelPoint _positionMax;

        [IgnoreDataMember]
        private int _widgetFontSize;

        [IgnoreDataMember]
        private string _widgetFontWeightLookupKey;

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
            _dateFormat = _dateFormatUserInput = restoredSettings.DateFormat;
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

                this.WhenAnyValue(x => x.DateFormatUserInput)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, nameof(DateFormat))
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

            Debug.WriteLine("Disposed of View Model.");
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

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}.");
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

        public void SetDateFormat(string newDateFormat, byte? ordinalDaySuffixPosition)
        {
            try
            {
                DateText = GetNewDateText(newDateFormat, ordinalDaySuffixPosition);
            }
            catch (System.FormatException)
            {
                throw;
            }

            DateFormat = newDateFormat;
            OrdinalDaySuffixPosition = ordinalDaySuffixPosition;
        }

        [DataMember]
        public PixelPoint Position
        {
            get => _widgetPosition;
            set => this.RaiseAndSetIfChanged(ref _widgetPosition, value);
        }

        [IgnoreDataMember]
        public PixelPoint PositionMax
        {
            get => _positionMax;
            set => this.RaiseAndSetIfChanged(ref _positionMax, value);
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
            get => _dateFormatUserInput;
            set => this.RaiseAndSetIfChanged(ref _dateFormatUserInput, value);
        }

        [DataMember]
        public string DateFormat
        { 
            get => _dateFormat;
            set => this.RaiseAndSetIfChanged(ref _dateFormat, value);
        }

        [DataMember]
        public byte? OrdinalDaySuffixPosition
        {
            get => _widgetOrdinalDaySuffixPosition;
            set => this.RaiseAndSetIfChanged(ref _widgetOrdinalDaySuffixPosition, value);
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
