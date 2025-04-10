﻿using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DateToday.ViewModels
{
    internal class SettingsViewModel : ReactiveValidationObject, IActivatableViewModel
    {
        private readonly WidgetViewModel _widgetViewModel;

        private int? _widgetPositionXUserInput, _widgetPositionYUserInput, _widgetFontSizeUserInput;

        private FontFamily _widgetFontFamily;
        private string _widgetFontWeightLookupKey;

        private string _widgetDateFormatUserInput;
        private byte? _widgetOrdinalDaySuffixPosition;

#pragma warning disable IDE0079
#pragma warning disable CA2213
        /* This disposable field is indeed disposed of with SettingsViewModel CompositeDisposables,
         * but the compiler doesn't recognise this. */

        private readonly ObservableAsPropertyHelper<PixelPoint> _widgetPositionMax;
#pragma warning restore CA2213, IDE0079

        private readonly List<ValidationHelper> _prerequisitesForNewDateFormatEntry;
        private ObservableAsPropertyHelper<EventPattern<DataErrorsChangedEventArgs>>?
            _dataErrorsChanged;
        private bool _isDateTextSetSuccessfully = true;

        public ViewModelActivator Activator { get; } = new();

        public ReactiveCommand<ValueTuple<string, byte?>, Unit> ParseDateFormatUserInput { get; }

        private EventPattern<DataErrorsChangedEventArgs>? DataErrorsChangedOAPH =>
            _dataErrorsChanged?.Value;

        public ReactiveCommand<bool, bool> CloseSettingsView { get; } =
            ReactiveCommand.Create<bool, bool>(dialogResult =>
            {
                /* This function will accept a dummy boolean value and pass it to the caller: the 
                 * WidgetViewModel instance. This behaviour is a vestige of cut functionality in 
                 * which the user would be able to manually save or revert changes to settings. I 
                 * will keep this here for now in case I want the SettingsWindow dialogue in the 
                 * future to return something meaningful. */

                return dialogResult;
            });

        public SettingsViewModel(WidgetViewModel widgetViewModel)
        {
            // TODO: Instead of injecting the actual WidgetViewModel object, simply inject an interface.

            _widgetViewModel = widgetViewModel;

            _widgetPositionXUserInput = widgetViewModel.Position.X;
            _widgetPositionYUserInput = widgetViewModel.Position.Y;

            _widgetPositionMax =
                _widgetViewModel.WhenAnyValue(widgetViewModel => widgetViewModel.PositionMax)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .ToProperty(this, nameof(WidgetPositionMax));

            _widgetFontSizeUserInput = widgetViewModel.FontSize;
            _widgetFontWeightLookupKey = widgetViewModel.FontWeightLookupKey;
            _widgetFontFamily = widgetViewModel.FontFamily;

            _widgetDateFormatUserInput = widgetViewModel.DateFormat;
            _widgetOrdinalDaySuffixPosition = widgetViewModel.OrdinalDaySuffixPosition;

            this.WhenActivated(disposables =>
            {
                disposables.Add(_widgetPositionMax);

                _widgetViewModel.WhenAnyValue(widgetViewModel => widgetViewModel.Position)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(position => position.X)
                                .BindTo(this, settingsViewModel =>
                                                  settingsViewModel.WidgetPositionXUserInput)
                                .DisposeWith(disposables);

                _widgetViewModel.WhenAnyValue(widgetViewModel => widgetViewModel.Position)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(position => position.Y)
                                .BindTo(this, settingsViewModel =>
                                                  settingsViewModel.WidgetPositionYUserInput)
                                .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetPositionXUserInput)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Where(input => input != null)
                    .Select(validatedInput => (int)validatedInput!)
                    .Select(positionX => widgetViewModel.Position.WithX(positionX))
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.Position)
                    .DisposeWith(disposables);

                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetPositionYUserInput)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Where(input => input != null)
                    .Select(validatedInput => (int)validatedInput!)
                    .Select(positionY => widgetViewModel.Position.WithY(positionY))
                    .BindTo(widgetViewModel, widgetViewModel => widgetViewModel.Position)
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

                _dataErrorsChanged =
                    Observable.FromEventPattern<DataErrorsChangedEventArgs>(
                        handler => ErrorsChanged += handler,
                        handler => ErrorsChanged -= handler
                        )
                        .ToProperty(this, nameof(DataErrorsChangedOAPH))
                        .DisposeWith(disposables);

                this.WhenAnyValue(
                        svm => svm.WidgetDateFormatUserInput,
                        svm => svm.WidgetOrdinalDaySuffixPosition)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Throttle(TimeSpan.FromMilliseconds(1))
                    .InvokeCommand(this, svm => svm.ParseDateFormatUserInput)
                    .DisposeWith(disposables);
            });

            IObservable<bool> isDateFormatPopulated =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.WidgetDateFormatUserInput)
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
                        settingsViewModel => 
                            settingsViewModel.WidgetDateFormatUserInput,
                        settingsViewModel => 
                            settingsViewModel.WidgetOrdinalDaySuffixPosition,
                    (dateFormat, suffixPosition) =>
                        !(suffixPosition != null && suffixPosition > dateFormat.Length))
                    .ObserveOn(RxApp.MainThreadScheduler);

            IObservable<bool> isDateFormatValid =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.IsDateTextSetSuccessfully)
                    .ObserveOn(RxApp.MainThreadScheduler);

            string[] curlyBraces = ["{", "}"];

            IObservable<bool> areCurlyBracesAbsentFromDateFormat =
                this.WhenAnyValue(
                        settingsViewModel => settingsViewModel.WidgetDateFormatUserInput, 
                        inputString => 
                            inputString != null && !curlyBraces.Any(inputString.Contains))
                    .ObserveOn(RxApp.MainThreadScheduler);

            // TODO: Deserialise validation strings from an external JSON file.

            _prerequisitesForNewDateFormatEntry = 
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
                isDateFormatValid,
                "The entered date format is invalid.\n" +
                "Please see Microsoft Learn: 'Custom date and time format strings'.");

            IObservable<bool> mayUserEnterNewDateFormat =
                this.WhenAnyValue(settingsViewModel => settingsViewModel.DataErrorsChangedOAPH)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(_ => _prerequisitesForNewDateFormatEntry
                                 .All(validaionRule => validaionRule.IsValid));

            ParseDateFormatUserInput =
                ReactiveCommand.Create<ValueTuple<string, byte?>>(
                    canExecute: mayUserEnterNewDateFormat,
                    execute: dateFormatTuple =>
                    {
                        try
                        {
                            _widgetViewModel.SetDateFormat(
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

        // TODO: Change read-only properties into ReactiveCommands.

        public static List<FontFamily> AvailableFonts =>
            // TODO: Should this list instead be injected into the constructor?

            [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

        public Dictionary<string, FontWeight> AvailableFontWeights =>
            _widgetViewModel.FontWeightDictionary;

        public PixelPoint WidgetPositionMax => _widgetPositionMax.Value;

        public int? WidgetPositionXUserInput
        {
            get => _widgetPositionXUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetPositionXUserInput, value);    
        }

        public int? WidgetPositionYUserInput
        {
            get => _widgetPositionYUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetPositionYUserInput, value);
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

        private bool IsDateTextSetSuccessfully
        {
            get => _isDateTextSetSuccessfully;
            set => this.RaiseAndSetIfChanged(ref _isDateTextSetSuccessfully, value);
        }
    }
}
