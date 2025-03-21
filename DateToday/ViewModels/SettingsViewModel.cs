using Avalonia;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DateToday.ViewModels
{
    internal class SettingsViewModel : ReactiveValidationObject, IActivatableViewModel
    {
        private readonly WidgetViewModel _widgetViewModel;
        private bool _isDateTextSetSuccessfully = true;

        private string _widgetDateFormatUserInput;
        private byte? _widgetOrdinalDaySuffixPositionUserInput;

        public SettingsViewModel(WidgetViewModel widgetViewModel)
        {
            // TODO: Instead of injecting the actual WidgetViewModel object, simply inject an interface.

            // TODO: Address style inconsistencies regarding the substring 'widget' in field names.

            _widgetViewModel = widgetViewModel;

            _widgetDateFormatUserInput = _widgetViewModel.DateFormat;
            _widgetOrdinalDaySuffixPositionUserInput = _widgetViewModel.OrdinalDaySuffixPosition;

            this.WhenActivated(disposables => 
            {
                _widgetViewModel.WhenAnyValue(x => x.PositionOAPH)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(x => x.X)
                                .ToProperty(this, nameof(WidgetPositionX))
                                .DisposeWith(disposables);

                _widgetViewModel.WhenAnyValue(x => x.PositionOAPH)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Select(x => x.Y)
                                .ToProperty(this, nameof(WidgetPositionY))
                                .DisposeWith(disposables);

                _widgetViewModel.WhenAnyValue(x => x.PositionMax)
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .ToProperty(this, nameof(WidgetPositionMax))
                                .DisposeWith(disposables);

                this.WhenAnyValue(
                        x => x.WidgetDateFormatUserInput, 
                        x => x.WidgetOrdinalDaySuffixPositionUserInput)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Throttle(TimeSpan.FromMilliseconds(1))
                    .Do(_ => OnDateFormatUserInputChanging())
                    .InvokeCommand(this, x => x.CommandParseDateFormatUserInput)
                    .DisposeWith(disposables);
            });

            IObservable<bool> mayUserEnterNewDateFormatObservable =
                this.WhenAnyValue(x => x.HasErrors)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x => !x);

            CommandParseDateFormatUserInput =
                ReactiveCommand.Create<ValueTuple<string, byte?>>(
                    canExecute: mayUserEnterNewDateFormatObservable, 
                    execute: x =>
                    {
                        try
                        {
                            _widgetViewModel.SetDateFormat(x.Item1, x.Item2);
                            
                        }
                        catch (System.FormatException)
                        {
                            /* The property IsDateTextSetSuccessfully is monitored such that the 
                                * user will be notified via a validation message when an error 
                                * occurs in the input date format. */

                            IsDateTextSetSuccessfully = false;
                        }
                        
                    });

            // TODO: Deserialise validation strings from an external JSON file.

            this.ValidationRule(
                settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                dateFormat => !string.IsNullOrEmpty(dateFormat),
                "Please enter a date format."); 

            IObservable<bool> isDateFormatOrdinalSuffixPositionValidObservable =
                this.WhenAnyValue(
                    x => x.WidgetDateFormatUserInput,
                    x => x.WidgetOrdinalDaySuffixPositionUserInput,
                    (dateFormat, suffixPosition) =>
                        !(suffixPosition != null && suffixPosition > dateFormat.Length))
                    .ObserveOn(RxApp.MainThreadScheduler);

            this.ValidationRule(
                settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                isDateFormatOrdinalSuffixPositionValidObservable,
                "Ordinal day suffifx position exceeds length of new date format.");

            string[] curlyBraces = ["{", "}"];

            this.ValidationRule(
                settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                dateFormat => !curlyBraces.Any(dateFormat!.Contains),
                "Curly braces are not permitted.");

            IObservable<bool> isDateFormatValidObservable =
                this.WhenAnyValue(x => x.IsDateTextSetSuccessfully)
                    .ObserveOn(RxApp.MainThreadScheduler);

            this.ValidationRule(
                settingsViewModel => settingsViewModel.WidgetDateFormatUserInput,
                isDateFormatValidObservable,
                "The entered date format is invalid.\n" +
                "Please see Microsoft Learn: 'Custom date and time format strings'.");   
        }

        private void OnDateFormatUserInputChanging()
        {
            // This method is executed immediately before CommandParseDateFormatUserInput.

            /* Reset the IsDateTextSetSuccessfully flag such that the user is permitted to 
             * correct an invalid date format. */

            IsDateTextSetSuccessfully = true;

            if (string.IsNullOrEmpty(WidgetDateFormatUserInput))
            {
                /* When the user erases the input date format, erase too the provided
                 * ordinal day suffix position. */

                WidgetOrdinalDaySuffixPositionUserInput = null;
            }
        }

        public int WidgetPositionX
        {
            get => _widgetViewModel.PositionOAPH.X;
            set
            {
                _widgetViewModel.Position = _widgetViewModel.PositionOAPH.WithX(value);
            }
        }

        public int WidgetPositionY
        {
            get => _widgetViewModel.PositionOAPH.Y;
            set
            {
                _widgetViewModel.Position = _widgetViewModel.PositionOAPH.WithY(value);
            }
        }

        public PixelPoint WidgetPositionMax => _widgetViewModel.PositionMax;

        public FontFamily WidgetFontFamily
        {
            get => _widgetViewModel.FontFamily;
            set => _widgetViewModel.FontFamily = value.Name;
        }

        public int? WidgetFontSize
        {
            get => _widgetViewModel.FontSize;
            set
            {
                if (value != null)
                {
                    _widgetViewModel.FontSize = (int)value;
                }
            }
        }

        public string WidgetFontWeightLookupKey
        {
            get => _widgetViewModel.FontWeightLookupKey;
            set 
            { 
                /* TODO: 
                 * 
                 *  Why does the XAML binding sometimes try to set this value to null?
                 *  Do I need to fix this? */

                if (!string.IsNullOrEmpty(value))
                { 
                    _widgetViewModel.FontWeightLookupKey = value;
                }  
            }
        }

        public string WidgetDateFormatUserInput
        {
            get => _widgetDateFormatUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetDateFormatUserInput, value);
        }

        public byte? WidgetOrdinalDaySuffixPositionUserInput
        {
            get => _widgetOrdinalDaySuffixPositionUserInput;
            set => this.RaiseAndSetIfChanged(ref _widgetOrdinalDaySuffixPositionUserInput, value);
        }

        private bool IsDateTextSetSuccessfully
        {
            get => _isDateTextSetSuccessfully;
            set => this.RaiseAndSetIfChanged(ref _isDateTextSetSuccessfully, value);
        }

        public static List<FontFamily> InstalledFontsList =>
            [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

        public Dictionary<string, FontWeight> FontWeightDictionary =>
            _widgetViewModel.FontWeightDictionary;

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ReactiveCommand<ValueTuple<string, byte?>, Unit> 
            CommandParseDateFormatUserInput { get; }

        public ReactiveCommand<bool, bool> CommandCloseSettingsView { get; } =
            ReactiveCommand.Create<bool, bool>(dialogResult =>
            {
                /* This function will accept a dummy boolean value and pass it to the caller: the 
                 * WidgetViewModel instance. This behaviour is a vestige of cut functionality in 
                 * which the user would be able to manually save or revert changes to settings. I 
                 * will keep this here for now in case I want the SettingsWindow dialogue in the 
                 * future to return something meaningful. */

                return dialogResult;
            });
    }
}
