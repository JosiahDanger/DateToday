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
    internal interface IWidgetViewModel
    {
        PixelPoint WindowPosition { get; set; }
        PixelPoint WindowPositionMax { get; }
        int FontSize { get; set; }
        FontFamily FontFamily { get; set; }
        string FontWeightLookupKey { get; set; }
        string DateFormat { get; set; }
        byte? OrdinalDaySuffixPosition { get; set; }

        void SetDateFormat(string newDateFormat, byte? ordinalDaySuffixPosition);
    }

    [DataContract]
    internal class WidgetViewModel : ReactiveObject, IActivatableViewModel, IWidgetViewModel
    {
        [IgnoreDataMember]
        private readonly WidgetModel _model;

        [IgnoreDataMember]
        private string _dateText, _dateFormat, _dateFormatUserInput, _fontWeightLookupKey;

        [IgnoreDataMember]
        private readonly ObservableAsPropertyHelper<int?> _fontWeight;

        [IgnoreDataMember]
        private FontFamily _fontFamily;

        [IgnoreDataMember]
        private PixelPoint _windowPosition, _windowPositionMax;

        [IgnoreDataMember]
        private int _fontSize;

        [IgnoreDataMember]
        private byte? _ordinalDaySuffixPosition;

        [IgnoreDataMember]
        public ViewModelActivator Activator { get; } = new();

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand ReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand ExitApplication { get; }

        public WidgetViewModel(
            IWidgetWindow viewInterface, 
            WidgetModel model,
            List<FontFamily> availableFonts,
            Dictionary<string, FontWeight> fontWeightDictionary,
            WidgetConfiguration restoredSettings)
        {
            _model = model;

            _dateText = string.Empty;

            _windowPosition = restoredSettings.WindowPosition;
            _fontSize = restoredSettings.FontSize;
            _fontWeightLookupKey = restoredSettings.FontWeightLookupKey;
            _dateFormat = _dateFormatUserInput = restoredSettings.DateFormat;
            _ordinalDaySuffixPosition = restoredSettings.OrdinalDaySuffixPosition;
            _fontFamily =
                /* Should the user have no settings persisted, the operating system's default font
                 * will be used. */
                restoredSettings.FontFamilyName ?? FontManager.Current.DefaultFontFamily;

            _fontWeight =
                this.WhenAnyValue(widgetViewModel => widgetViewModel.FontWeightLookupKey)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(key => AttemptFontWeightLookup(fontWeightDictionary, key))
                    .ToProperty(this, nameof(FontWeight));

            this.WhenActivated(disposables =>
            {
                disposables.Add(_fontWeight); // Probably doesn't need explicit disposal?
                disposables.Add(_model);

                this.HandleActivation();

                _model.NewMinuteEventObservable?
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => RefreshDateText())
                      .DisposeWith(disposables);
            });

            ReceiveNewSettings = ReactiveCommand.CreateFromTask(async () =>
            {
                SettingsViewModel settingsViewModel = 
                    new(this, availableFonts, fontWeightDictionary);
                await InteractionReceiveNewSettings.Handle(settingsViewModel);

                RxApp.SuspensionHost.AppState = this;
            });

            ExitApplication = ReactiveCommand.Create(() =>
            {
                viewInterface?.Close(0);
            });
        }

        private void HandleActivation()
        {
            RefreshDateText();
        }

        private void RefreshDateText()
        {
            // TODO: This could probably be a ReactiveCommand or ICommand.

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
            // TODO: Make this a ReactiveCommand?

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

        [IgnoreDataMember]
        public int? FontWeight => _fontWeight.Value; // TODO: Make this a ReactiveCommand?

        [DataMember]
        public PixelPoint WindowPosition
        {
            get => _windowPosition;
            set => this.RaiseAndSetIfChanged(ref _windowPosition, value);
        }

        [IgnoreDataMember]
        public PixelPoint WindowPositionMax
        {
            get => _windowPositionMax;
            set => this.RaiseAndSetIfChanged(ref _windowPositionMax, value);
        }

        [DataMember]
        private string FontFamilyName
        {
            /* This property exists because the ReactiveUI data persistence functionality doesn't 
             * support Avalonia's FontFamily data type. */

            get => _fontFamily.Name;
        }

        [IgnoreDataMember]
        public FontFamily FontFamily
        {
            get => _fontFamily;
            set => this.RaiseAndSetIfChanged(ref _fontFamily, value);
        }

        [DataMember]
        public int FontSize
        {
            get => _fontSize;
            set => this.RaiseAndSetIfChanged(ref _fontSize, value);
        }

        [DataMember]
        public string FontWeightLookupKey
        {
            get => _fontWeightLookupKey;
            set => this.RaiseAndSetIfChanged(ref _fontWeightLookupKey, value);
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
            get => _ordinalDaySuffixPosition;
            set => this.RaiseAndSetIfChanged(ref _ordinalDaySuffixPosition, value);
        }

        [IgnoreDataMember]
        public string DateText 
        { 
            get => _dateText;
            set => this.RaiseAndSetIfChanged(ref _dateText, value);
        }
    }
}
