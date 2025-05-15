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
        Point WidgetPosition { get; set; }
        Point WidgetPositionMax { get; }
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
        [IgnoreDataMember]
        private readonly INewMinuteEventGenerator _modelInterface;

        [IgnoreDataMember]
        private string
            _dateText = string.Empty, _dateFormat, _dateFormatUserInput, _fontWeightLookupKey;

        [IgnoreDataMember]
        private readonly ObservableAsPropertyHelper<int?> _fontWeight;

        [IgnoreDataMember]
        private FontFamily _fontFamily;

        [IgnoreDataMember]
        private readonly Color _automaticFontColour;

        [IgnoreDataMember]
        private Color? _customFontColour, _customDropShadowColour;

        [IgnoreDataMember]
        private bool _isDropShadowEnabled;

        [IgnoreDataMember]
        private DropShadowEffect? _dropShadow;

        [IgnoreDataMember]
        private Point _widgetPosition, _widgetPositionMax;

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
            INewMinuteEventGenerator modelInterface,
            List<FontFamily> availableFonts,
            Dictionary<string, FontWeight> fontWeightDictionary,
            WidgetConfiguration restoredSettings)
        {
            /* Assigning the 'modelInterface' argument to a private WidgetViewModel field here is
             * not strictly necessary; the argument object can be disposed of with the
             * WidgetViewModel CompositeDisposable collection. However, if I don't assign it to a
             * WidgetViewModel field, the compiler raises warning CA2000. I suspect that this
             * warning is raised in error, but I'm not certain. */

            _modelInterface = modelInterface;
            _automaticFontColour = viewInterface.ThemedTextColour;

            _widgetPosition = restoredSettings.WidgetPosition;
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
                    .Select(key => AttemptFontWeightLookup(fontWeightDictionary, key))
                    .ToProperty(this, nameof(FontWeight));

            this.WhenActivated(disposables =>
            {
                disposables.Add(_modelInterface);

                this.HandleActivation();

                _modelInterface.NewMinuteEventObservable?
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .Subscribe(_ => RefreshDateText())
                               .DisposeWith(disposables);

                this.WhenAnyValue(widgetViewModel => widgetViewModel.IsDropShadowEnabled)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(isEnabled =>
                        IsDropShadowEnabled_OnChange(
                            isEnabled, viewInterface.ThemedTextShadowColour));

                this.WhenAnyValue(widgetViewModel => widgetViewModel.CustomDropShadowColour)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(newColourOrNull =>
                        CustomDropShadowColour_OnChange(
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

        private void IsDropShadowEnabled_OnChange(bool isEnabled, Color initialColour)
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

        private void CustomDropShadowColour_OnChange(Color? newColourOrNull, Color fallback)
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

        public void SetDateFormat(string newDateFormat, byte? ordinalDaySuffixPosition)
        {
            // TODO: Make this a Command?

            try
            {
                DateText = GetNewDateText(newDateFormat, ordinalDaySuffixPosition);
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

        [IgnoreDataMember]
        public int? FontWeight => _fontWeight.Value;

        [IgnoreDataMember]
        public Color AutomaticFontColour => _automaticFontColour;

        [DataMember]
        public Point WidgetPosition
        {
            get => _widgetPosition;
            set => this.RaiseAndSetIfChanged(ref _widgetPosition, value);
        }

        [IgnoreDataMember]
        public Point WidgetPositionMax
        {
            get => _widgetPositionMax;
            set => this.RaiseAndSetIfChanged(ref _widgetPositionMax, value);
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
