using Avalonia;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Enums;
using DateToday.Models;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    internal interface IWidgetViewModel
    {
        Point AnchoredCornerScaledPosition { get; set; }
        Point AnchoredCornerScaledPositionMax { get; set; }
        WindowVertexIdentifier AnchoredCorner { get; set; }
        int FontSize { get; set; }
        FontFamily FontFamily { get; set; }
        string FontWeightLookupKey { get; set; }
        Color? CustomFontColour { get; set; }
        bool IsDropShadowEnabled { get; set; }
        Color? CustomDropShadowColour { get; set; }
        string DateFormat { get; set; }
        byte? OrdinalDaySuffixPosition { get; set; }

        void SetDateFormat(string newDateFormat, byte? ordinalDaySuffixPosition);
    }

    [DataContract]
    internal sealed class WidgetViewModel : ReactiveObject, IActivatableViewModel, IWidgetViewModel
    {
        private readonly INewMinuteEventGenerator _modelInterface;

        private readonly CultureInfo _culture;

        private string
            _dateText = string.Empty, _dateFormat, _dateFormatUserInput, _fontWeightLookupKey;

        private readonly ObservableAsPropertyHelper<FontWeight> _fontWeight;

        private FontFamily _fontFamily;

        private readonly Color _automaticFontColour;

        private Color? _customFontColour, _customDropShadowColour;

        private bool _isDropShadowEnabled;

        private DropShadowEffect? _dropShadow;

        private Point _anchoredCornerScaledPosition, _anchoredCornerScaledPositionMax;

        private WindowVertexIdentifier _anchoredCorner;

        private int _fontSize;

        private byte? _ordinalDaySuffixPosition;

        public ViewModelActivator Activator { get; } = new();

        public Interaction<SettingsViewModel, Unit> InteractionReceiveNewSettings { get; } = new();

        public ICommand ReceiveNewSettings { get; }

        public ICommand ExitApplication { get; }

        public WidgetViewModel(
            IWidgetWindow viewInterface,
            INewMinuteEventGenerator modelInterface,
            List<FontFamily> availableFonts,
            Dictionary<string, FontWeight> fontWeightDictionary,
            WidgetConfiguration restoredSettings,
            CultureInfo culture)
        {
            /* Assigning the 'modelInterface' argument to a private WidgetViewModel field here is
             * not strictly necessary; the argument object can be disposed of with the
             * WidgetViewModel CompositeDisposable collection. However, if I don't assign it to a
             * WidgetViewModel field, the compiler raises warning CA2000. I suspect that this
             * warning is raised in error, but I'm not certain. */

            _modelInterface = modelInterface;
            _culture = culture;
            _automaticFontColour = viewInterface.ThemedTextColour;

            _anchoredCornerScaledPosition = restoredSettings.AnchoredCornerScaledPosition;
            _anchoredCorner = restoredSettings.AnchoredCorner;
            _fontSize = restoredSettings.FontSize;
            _fontWeightLookupKey = restoredSettings.FontWeightLookupKey;
            _customFontColour = restoredSettings.CustomFontColour;
            _isDropShadowEnabled = restoredSettings.IsDropShadowEnabled;
            _customDropShadowColour = restoredSettings.CustomDropShadowColour;
            _dateFormat = _dateFormatUserInput = restoredSettings.DateFormat;
            _ordinalDaySuffixPosition = restoredSettings.OrdinalDaySuffixPosition;
            _fontFamily =
                /* Should the user have no settings persisted, the operating system's default font
                 * will be used. */
                restoredSettings.FontFamilyName ?? FontManager.Current.DefaultFontFamily;

            _fontWeight =
                /* Please note that _fontWeight does not need explicit disposal.
                 * See: https://www.reactiveui.net/docs/guidelines/framework/dispose-your-subscriptions.html */

                this.WhenAnyValue(widgetViewModel => widgetViewModel.FontWeightLookupKey)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(key => 
                                AttemptFontWeightLookup(
                                    fontWeightDictionary, key, FontWeight.Normal))
                    .ToProperty(this, nameof(FontWeight));

            this.WhenActivated(disposables =>
            {
                disposables.Add(_modelInterface);

                RefreshDateText();

                _modelInterface.NewMinuteEventObservable
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .Subscribe(_ => RefreshDateText())
                               .DisposeWith(disposables);

                this.WhenAnyValue(widgetViewModel => widgetViewModel.IsDropShadowEnabled)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(isEnabled =>
                        IsDropShadowEnabled_Changed(
                            isEnabled, viewInterface.ThemedTextShadowColour));

                this.WhenAnyValue(widgetViewModel => widgetViewModel.CustomDropShadowColour)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(newColourOrNull =>
                        CustomDropShadowColour_Changed(
                            newColourOrNull, viewInterface.ThemedTextShadowColour));
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

        private void RefreshDateText()
        {
            // TODO: This could probably be a ReactiveCommand or ICommand.

            DateText = GetNewDateText(DateFormat, OrdinalDaySuffixPosition, _culture);
        }

        private static string GetNewDateText(
            string dateFormat, byte? ordinalDaySuffixPosition, CultureInfo culture)
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
                    currentDateTime.ToString(dateFormatIncludingFormatItem, culture);

                formattedDateOutput =
                    string.Format(culture, formattedDateIncludingFormatItem, ordinalDaySuffix);
            }
            else
            {
                formattedDateOutput = currentDateTime.ToString(dateFormat, culture);
            }

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}.");
            return formattedDateOutput;
        }

        public void SetDateFormat(string newDateFormat, byte? ordinalDaySuffixPosition)
        {
            // TODO: Make this a Command?

            try
            {
                DateText = GetNewDateText(newDateFormat, ordinalDaySuffixPosition, _culture);
            }
            catch (System.FormatException)
            {
                // The provided date format or ordinal day suffix position are invalid.
                throw;
            }

            /* A new valid date format, which may include an ordinal day suffix, has been applied 
             * successfully. The method arguments are persisted here. */

            DateFormat = newDateFormat;
            OrdinalDaySuffixPosition = ordinalDaySuffixPosition;
        }

        private static FontWeight AttemptFontWeightLookup(
            Dictionary<string, FontWeight>? fontWeightDictionary, string lookupKey, 
            FontWeight fallback)
        {
            if (fontWeightDictionary != null && !string.IsNullOrEmpty(lookupKey))
            {
                bool isFontWeightValueFound =
                    fontWeightDictionary.TryGetValue(lookupKey, out FontWeight newFontWeightValue);

                if (isFontWeightValueFound)
                {
                    return newFontWeightValue;
                }
            }

            Debug.WriteLine(
                $"Failed to discern font weight value associated with key: '{lookupKey}'. Using " +
                $"{fallback} instead.");

            return fallback;
        }

        private void IsDropShadowEnabled_Changed(bool isEnabled, Color initialColour)
        {
            if (isEnabled)
            {
                DropShadow =
                    new DropShadowEffect()
                    {
                        BlurRadius = 0,
                        Color = initialColour
                    };
            }
            else
            {
                DropShadow = null;
            }
        }

        private void CustomDropShadowColour_Changed(Color? newColourOrNull, Color fallback)
        {
            if (DropShadow != null)
            {
                if (newColourOrNull is Color newColour)
                {
                    DropShadow.Color = newColour;
                }
                else
                {
                    DropShadow.Color = fallback;
                }
            }
        }

        [IgnoreDataMember]
        public FontWeight FontWeight => _fontWeight.Value;

        [IgnoreDataMember]
        public Color AutomaticFontColour => _automaticFontColour;

        [DataMember]
        public Point AnchoredCornerScaledPosition
        {
            get => _anchoredCornerScaledPosition;
            set => this.RaiseAndSetIfChanged(ref _anchoredCornerScaledPosition, value);
        }

        [IgnoreDataMember]
        public Point AnchoredCornerScaledPositionMax
        {
            get => _anchoredCornerScaledPositionMax;
            set => this.RaiseAndSetIfChanged(ref _anchoredCornerScaledPositionMax, value);
        }

        [DataMember]
        public WindowVertexIdentifier AnchoredCorner
        {
            get => _anchoredCorner;
            set => this.RaiseAndSetIfChanged(ref _anchoredCorner, value);
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

        [DataMember]
        public Color? CustomFontColour
        {
            get => _customFontColour;
            set => this.RaiseAndSetIfChanged(ref _customFontColour, value);
        }

        [DataMember]
        public bool IsDropShadowEnabled
        {
            get => _isDropShadowEnabled;
            set => this.RaiseAndSetIfChanged(ref _isDropShadowEnabled, value);
        }

        [DataMember]
        public Color? CustomDropShadowColour
        {
            get => _customDropShadowColour;
            set => this.RaiseAndSetIfChanged(ref _customDropShadowColour, value);
        }

        [IgnoreDataMember]
        public DropShadowEffect? DropShadow
        {
            get => _dropShadow;
            set => this.RaiseAndSetIfChanged(ref _dropShadow, value);
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
