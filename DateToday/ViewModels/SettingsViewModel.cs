using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Enums;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;

namespace DateToday.ViewModels
{
    internal sealed class SettingsViewModel : ReactiveValidationObject, IActivatableViewModel
    {
        private int? _widgetFontSizeUserInput;
        private byte? _widgetOrdinalDaySuffixPosition;

        private double? _widgetPositionUserInputX, _widgetPositionUserInputY;
        private WindowVertexIdentifier _widgetAnchoredCorner;

        private FontFamily _widgetFontFamily;
        private string _widgetFontWeightLookupKey, _widgetDateFormatUserInput;
        private string _settingsExitButtonContent = string.Empty;

        private readonly List<FontFamily> _availableFonts;
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        private Color? _widgetCustomFontColour, _widgetCustomDropShadowColour;

        private bool
            _isWidgetFontColourAutomatic, _isWidgetDropShadowEnabled,
            _isWidgetDropShadowColourAutomatic;

#pragma warning disable IDE0079
#pragma warning disable CA2213
        /* This disposable field is indeed disposed of with SettingsViewModel CompositeDisposables,
         * but the compiler doesn't care, and throws warning CA2213 anyway. */

        private readonly ObservableAsPropertyHelper<Point> _widgetPositionMax;
#pragma warning restore CA2213, IDE0079

        private ObservableAsPropertyHelper<EventPattern<DataErrorsChangedEventArgs>>?
            _dataErrorsChanged;
        
        public ViewModelActivator Activator { get; } = new();

        private bool _isDateTextSetSuccessfully = true;

        public ReactiveCommand<ValueTuple<string, byte?>, Unit> ParseDateFormatUserInput { get; }

        public ReactiveCommand<Unit, Unit> CloseWidgetSettings { get; } = 
            ReactiveCommand.Create(() => Unit.Default);

        public SettingsViewModel(
            IWidgetViewModel widgetViewModel,
            List<FontFamily> availableFonts,
            Dictionary<string, FontWeight> fontWeightDictionary)
        {
            _availableFonts = availableFonts;
            _fontWeightDictionary = fontWeightDictionary;

            _widgetPositionUserInputX = widgetViewModel.AnchoredCornerScaledPosition.X;
            _widgetPositionUserInputY = widgetViewModel.AnchoredCornerScaledPosition.Y;

            _widgetAnchoredCorner = widgetViewModel.AnchoredCorner;

            _widgetFontSizeUserInput = widgetViewModel.FontSize;
            _widgetFontFamily = widgetViewModel.FontFamily;
            _widgetFontWeightLookupKey = widgetViewModel.FontWeightLookupKey;

            _isWidgetFontColourAutomatic = widgetViewModel.CustomFontColour == null;
            _widgetCustomFontColour = widgetViewModel.CustomFontColour;

            _isWidgetDropShadowEnabled = widgetViewModel.IsDropShadowEnabled;
            _isWidgetDropShadowColourAutomatic = widgetViewModel.CustomDropShadowColour == null;
            _widgetCustomDropShadowColour = widgetViewModel.CustomDropShadowColour;

            _widgetDateFormatUserInput = widgetViewModel.DateFormat;
            _widgetOrdinalDaySuffixPosition = widgetViewModel.OrdinalDaySuffixPosition;

            _widgetPositionMax = 
                widgetViewModel.WhenAnyValue(wvm => wvm.AnchoredCornerScaledPositionMax)
                               .ObserveOn(RxApp.MainThreadScheduler)
                               .ToProperty(this, nameof(WidgetPositionMax));

            this.WhenActivated(disposables =>
            {
                disposables.Add(_widgetPositionMax);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetPositionUserInputX)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Where(input => input != null)
                    .Select(validatedInput => (double)validatedInput!)
                    .Select(positionX => 
                                widgetViewModel.AnchoredCornerScaledPosition.WithX(positionX))
                    .BindTo(widgetViewModel, wvm => wvm.AnchoredCornerScaledPosition)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetPositionUserInputY)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Where(input => input != null)
                    .Select(validatedInput => (double)validatedInput!)
                    .Select(positionY => 
                                widgetViewModel.AnchoredCornerScaledPosition.WithY(positionY))
                    .BindTo(widgetViewModel, wvm => wvm.AnchoredCornerScaledPosition)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetAnchoredCorner)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.AnchoredCorner)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetFontFamily)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.FontFamily)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetFontSizeUserInput)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Where(input => input != null)
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.FontSize)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetFontWeightLookupKey)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.FontWeightLookupKey)
                    .DisposeWith(disposables);

                this.WhenAnyValue(svm => svm.IsWidgetFontColourAutomatic)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(isAutomatic => { 
                        if (isAutomatic)
                        {
                            WidgetCustomFontColour = null;
                        }
                    });

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetCustomFontColour)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.CustomFontColour)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.IsWidgetDropShadowEnabled)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Do(isEnabled => {
                        if (!isEnabled)
                        {
                            IsWidgetDropShadowColourAutomatic = true;
                        }
                    })
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.IsDropShadowEnabled)
                    .DisposeWith(disposables);

                this.WhenAnyValue(svm => svm.IsWidgetDropShadowColourAutomatic)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(isAutomatic => {
                        if (isAutomatic)
                        {
                            WidgetCustomDropShadowColour = null;
                        }
                    });

                this.WhenAnyValue(svm => svm.WidgetCustomDropShadowColour)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .BindTo(widgetViewModel, wvm => wvm.CustomDropShadowColour)
                    .DisposeWith(disposables);

                _dataErrorsChanged =
                    // Does not need explicit disposal.
                    Observable.FromEventPattern<DataErrorsChangedEventArgs>(
                        handler => ErrorsChanged += handler,
                        handler => ErrorsChanged -= handler
                    )
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, nameof(DataErrorsChanged));

                this.WhenAnyValue(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                        settingsViewModel => settingsViewModel.WidgetOrdinalDaySuffixPosition)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)

                    /* TODO: 
                     * https://stackoverflow.com/questions/29636910/possible-to-ignore-the-initial-value-for-a-reactiveobject */

                    .Skip(1)
                    .Throttle(TimeSpan.FromMilliseconds(1))
                    .InvokeCommand(this, svm => svm.ParseDateFormatUserInput);
            });

            IObservable<bool> isDateFormatPopulated =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetDateFormatUserInput)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(inputString => !string.IsNullOrEmpty(inputString))
                    .Do(isPopulated => {
                        if (!isPopulated)
                        {
                            /* When the user erases the input date format, erase too the provided
                             * ordinal day suffix position. */

                            WidgetOrdinalDaySuffixPosition = null;
                        }
                    });

            IObservable<bool> isDateFormatOrdinalSuffixPositionValid =
                this.WhenAnyValue(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                        settingsViewModel => settingsViewModel.WidgetOrdinalDaySuffixPosition,
                        (dateFormat, suffixPosition) =>
                            !(suffixPosition != null && suffixPosition > dateFormat.Length))
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler);

            IObservable<bool> isDateFormatValid =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.IsDateTextSetSuccessfully)
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler);

            string[] curlyBraces = ["{", "}"];

            IObservable<bool> areCurlyBracesAbsentFromDateFormat =
                this.WhenAnyValue(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput, 
                        inputString => 
                            inputString != null && !curlyBraces.Any(inputString.Contains))
                    // Does not need explicit disposal.
                    .ObserveOn(RxApp.MainThreadScheduler);

            // TODO: Deserialise validation strings from an external JSON file.

            List<ValidationHelper> prerequisitesForNewDateFormatEntry = 
                [
                    this.ValidationRule(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                        isDateFormatPopulated,
                        "Please enter a date format."),

                    this.ValidationRule(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                        isDateFormatOrdinalSuffixPositionValid,
                        "Ordinal day suffifx position exceeds length of new date format."),

                    this.ValidationRule(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                        areCurlyBracesAbsentFromDateFormat,
                        "Curly braces are not permitted.")
                ];

            this.ValidationRule(
                settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                isDateFormatValid, "The entered date format is invalid.");

            IObservable<bool> mayUserEnterNewDateFormat =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.DataErrorsChanged)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(_ => prerequisitesForNewDateFormatEntry
                                 .All(validaionRule => validaionRule.IsValid));

            ParseDateFormatUserInput =
                ReactiveCommand.Create<ValueTuple<string, byte?>>(
                    canExecute: mayUserEnterNewDateFormat,
                    execute: dateFormatTuple =>
                    {
                        try
                        {
                            widgetViewModel.SetDateFormat(
                                dateFormatTuple.Item1, dateFormatTuple.Item2);
                            IsDateTextSetSuccessfully = true;
                        }
                        catch (System.FormatException)
                        {
                            /* The property IsDateTextSetSuccessfully is monitored such that the 
                             * user will be notified via a validation message when an error 
                             * occurs in the input date format. */

                            IsDateTextSetSuccessfully = false;
                        }
                    });
        }

        private static string? GetProductVersion()
        {
            Assembly? processExecutable = Assembly.GetEntryAssembly();

            if (processExecutable != null)
            {
                string? productVersion = 
                    FileVersionInfo.GetVersionInfo(processExecutable.Location).ProductVersion;

                if (productVersion != null)
                {
                    return $"Ver. {productVersion}";
                }
            }

            return null;
        }

        public static string? ProductVersion => GetProductVersion();

        private EventPattern<DataErrorsChangedEventArgs>? DataErrorsChanged =>
            _dataErrorsChanged?.Value;

        public List<FontFamily> AvailableFonts => _availableFonts;

        public Dictionary<string, FontWeight> AvailableFontWeights => _fontWeightDictionary;

        public Point WidgetPositionMax => _widgetPositionMax.Value;

        public double? WidgetPositionUserInputX
        {
            get => _widgetPositionUserInputX;
            set => this.RaiseAndSetIfChanged(ref _widgetPositionUserInputX, value);    
        }

        public double? WidgetPositionUserInputY
        {
            get => _widgetPositionUserInputY;
            set => this.RaiseAndSetIfChanged(ref _widgetPositionUserInputY, value);
        }

        public WindowVertexIdentifier WidgetAnchoredCorner
        {
            get => _widgetAnchoredCorner;
            set => this.RaiseAndSetIfChanged(ref _widgetAnchoredCorner, value);
        }

        public FontFamily WidgetFontFamily
        {
            get => _widgetFontFamily;
            set => this.RaiseAndSetIfChanged(ref _widgetFontFamily, value);
        }

        public int? WidgetFontSizeUserInput
        {
            get => _widgetFontSizeUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetFontSizeUserInput, value);
        }

        public string WidgetFontWeightLookupKey
        {
            get => _widgetFontWeightLookupKey;
            set => this.RaiseAndSetIfChanged(ref _widgetFontWeightLookupKey, value);
        }

        public Color? WidgetCustomFontColour
        {
            get => _widgetCustomFontColour;
            set => this.RaiseAndSetIfChanged(ref _widgetCustomFontColour, value);
        }

        public bool IsWidgetFontColourAutomatic
        {
            get => _isWidgetFontColourAutomatic;
            set => this.RaiseAndSetIfChanged(ref _isWidgetFontColourAutomatic, value);
        }

        public bool IsWidgetDropShadowEnabled
        {
            get => _isWidgetDropShadowEnabled;
            set => this.RaiseAndSetIfChanged(ref _isWidgetDropShadowEnabled, value);
        }

        public bool IsWidgetDropShadowColourAutomatic
        {
            get => _isWidgetDropShadowColourAutomatic;
            set => this.RaiseAndSetIfChanged(ref _isWidgetDropShadowColourAutomatic, value);
        }

        public Color? WidgetCustomDropShadowColour
        {
            get => _widgetCustomDropShadowColour;
            set => this.RaiseAndSetIfChanged(ref _widgetCustomDropShadowColour, value);
        }

        public string WidgetDateFormatUserInput
        {
            get => _widgetDateFormatUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetDateFormatUserInput, value);
        }

        public byte? WidgetOrdinalDaySuffixPosition
        {
            get => _widgetOrdinalDaySuffixPosition;
            set => this.RaiseAndSetIfChanged(ref _widgetOrdinalDaySuffixPosition, value);
        }

        public string SettingsExitButtonContent
        {
            get => _settingsExitButtonContent;
            set => this.RaiseAndSetIfChanged(ref _settingsExitButtonContent, value);
        }

        private bool IsDateTextSetSuccessfully
        {
            get => _isDateTextSetSuccessfully;
            set => this.RaiseAndSetIfChanged(ref _isDateTextSetSuccessfully, value);
        }
    }
}
