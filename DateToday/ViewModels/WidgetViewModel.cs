﻿using Avalonia;
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
    internal class WidgetViewModel : ReactiveObject, IActivatableViewModel
    {
        [IgnoreDataMember]
        private readonly IWidgetView _viewInterface;

        [IgnoreDataMember]
        private readonly Dictionary<string, FontWeight> _fontWeightDictionary;

        [IgnoreDataMember]
        private readonly WidgetModel _model;

        [IgnoreDataMember]
        private string _dateText, _dateFormat, _dateFormatUserInput, _fontWeightLookupKey;

        [IgnoreDataMember]
        private ObservableAsPropertyHelper<int?>? _fontWeight;

        [IgnoreDataMember]
        private FontFamily _fontFamily;

        [IgnoreDataMember]
        private PixelPoint _position, _positionMax;

        [IgnoreDataMember]
        private int _fontSize;

        [IgnoreDataMember]
        private byte? _ordinalDaySuffixPosition;

        public ViewModelActivator Activator { get; } = new();

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand ReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand ExitApplication { get; }

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
            _fontFamily = 
                restoredSettings.FontFamilyName ?? FontManager.Current.DefaultFontFamily;

            _position = restoredSettings.Position;
            _fontSize = restoredSettings.FontSize;
            _fontWeightLookupKey = restoredSettings.FontWeightLookupKey;
            _dateFormat = _dateFormatUserInput = restoredSettings.DateFormat;
            _ordinalDaySuffixPosition = restoredSettings.OrdinalDaySuffixPosition;

            this.WhenActivated(disposables =>
            {
                this.HandleActivation();

                _model.NewMinuteEventObservable?
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => RefreshDateText())
                      .DisposeWith(disposables);

                _fontWeight =
                    this.WhenAnyValue(x => x.FontWeightLookupKey)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Select(x => AttemptFontWeightLookup(fontWeightDictionary, x))
                        .ToProperty(this, nameof(FontWeight))
                        .DisposeWith(disposables);
            });

            ReceiveNewSettings = ReactiveCommand.CreateFromTask(async () =>
            {
                SettingsViewModel settingsViewModel = new(this);
                await InteractionReceiveNewSettings.Handle(settingsViewModel);

                RxApp.SuspensionHost.AppState = this;
            });

            ExitApplication = ReactiveCommand.Create(() =>
            {
                _viewInterface?.CloseView(0);
            });
        }

        private void HandleActivation()
        {
            RefreshDateText();
        }

        private void RefreshDateText()
        {
            // TODO: This could probably be a ReactiveCommand.

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
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
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
        public int? FontWeight => _fontWeight?.Value;

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

        [IgnoreDataMember]
        public Dictionary<string, FontWeight> FontWeightDictionary => _fontWeightDictionary;
    }
}
