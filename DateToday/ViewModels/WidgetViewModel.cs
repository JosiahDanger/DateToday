using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Media;
using DateToday.Models;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace DateToday.ViewModels
{
    [DataContract]
    public class WidgetViewModel : ViewModelBase
    {
        [IgnoreDataMember]
        private IWidgetView? _viewInterface;

        [IgnoreDataMember]
        private readonly WidgetModel _model = new();

        [IgnoreDataMember]
        private string _dateText = GetNewWidgetText();

        [IgnoreDataMember]
        private FontFamily _widgetFontFamily = FontManager.Current.DefaultFontFamily;

        [IgnoreDataMember]
        private string _widgetFontFamilyName = FontManager.Current.DefaultFontFamily.Name;

        [IgnoreDataMember]
        private PixelPoint _widgetPosition = PixelPoint.Origin;

        public WidgetViewModel()
        {
            _model.ObservableNewMinuteEvent?
                .Subscribe
                (
                    _ => HandleNewMinuteEvent()
                );

            CommandReceiveNewSettings = ReactiveCommand.CreateFromTask(async () =>
                {
                    SettingsViewModel settingsViewModel = new(this);
                    await InteractionReceiveNewSettings.Handle(settingsViewModel);

                    RxApp.SuspensionHost.AppState = this;
                });

            CommandExitApplication = ReactiveCommand.Create(() =>
                {
                    if (Application.Current?.ApplicationLifetime is 
                        IClassicDesktopStyleApplicationLifetime desktopApp)
                    {
                        desktopApp.Shutdown();
                    }
                });
        }

        public void AttachViewInterface(IWidgetView newViewInterface)
        {
            _viewInterface = newViewInterface;
            newViewInterface.WidgetPosition = _widgetPosition;
        }

        private static string GetNewWidgetText()
        {
            static string GetDaySuffix(int dayNumberInWeek)
            {
                return dayNumberInWeek switch
                {
                    1 or 21 or 31 => "st",
                    2 or 22 => "nd",
                    3 or 23 => "rd",
                    _ => "th",
                };
            }

            const string DATE_TEXT_FORMAT = "dddd', the 'd'{0} of 'MMMM";

            DateTime currentDateTime = DateTime.Now;
            Byte dayOfMonth = (byte) currentDateTime.Day;
            string daySuffix = GetDaySuffix(dayOfMonth);

            string dateTextBuffer = currentDateTime.ToString(DATE_TEXT_FORMAT);

            Debug.WriteLine($"Refreshed widget text at {currentDateTime}");

            return string.Format(dateTextBuffer, daySuffix);
        }

        private void HandleNewMinuteEvent()
        {
            /* TODO: 
             * This behaviour should be altered such that the Model resets its tick generator 
             * interval independently of the View Model. I think that this is an example of business
             * logic, which should be encapsulated within the model. */

            _model.ResetTickGeneratorInterval();
            _dateText = GetNewWidgetText();
        }

        private static void ValidateFontFamilyName(string inputFontFamilyName)
        {
            static bool DoesFontFamilyExist(string targetFontFamilyName)
            {
                return FontManager.Current.SystemFonts.Any(
                    currentfontFamily => string.Equals(
                        targetFontFamilyName,
                        currentfontFamily.Name,
                        StringComparison.OrdinalIgnoreCase
                    ));
            }

            // TODO: Make application-wide string constants including validation text.

            if (string.IsNullOrEmpty(inputFontFamilyName))
            {
                throw new DataValidationException("This field is required.");
            }
            else if (!DoesFontFamilyExist(inputFontFamilyName))
            {
                throw new DataValidationException(
                    $"Font '{inputFontFamilyName}' is not installed."
                );
            }
        }

        [IgnoreDataMember]
        public IWidgetView? ViewInterface
        {
            set => _viewInterface = value;
        }

        [DataMember]
        public PixelPoint WidgetPosition
        {
            /* The private field _widgetPosition exists on this View Model as a reflection of its
             * View's Position property. This behaviour exists in order to facilitate ReactiveUI's
             * data persistence functionality: it is apparently very stupid, and refuses to save the
             * current Position value when I access it via the IWidgetView interface. */

            get => _widgetPosition;
            set
            {
                _widgetPosition = value;

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

        [IgnoreDataMember]
        public FontFamily WidgetFontFamily
        {
            get => _widgetFontFamily;
            set => this.RaiseAndSetIfChanged(ref _widgetFontFamily, value);

            // TODO: Upon changing font, discard from memory the font selected previously.
        }

        [DataMember]
        public string WidgetFontFamilyName
        {
            get => _widgetFontFamilyName;
            set
            {
                ValidateFontFamilyName(value);

                WidgetFontFamily = value;
                _widgetFontFamilyName = _widgetFontFamily.Name;
            }
        }

        [IgnoreDataMember]
        public Interaction<SettingsViewModel, bool> InteractionReceiveNewSettings { get; } = new();

        [IgnoreDataMember]
        public ICommand CommandReceiveNewSettings { get; }

        [IgnoreDataMember]
        public ICommand CommandExitApplication { get; }

        [IgnoreDataMember]
        public string DateText { get => _dateText; }
    }
}
