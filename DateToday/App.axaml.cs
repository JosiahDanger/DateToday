using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Drivers;
using DateToday.Models;
using DateToday.ViewModels;
using DateToday.Views;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static DateToday.Utilities;

namespace DateToday
{
    internal sealed partial class App : Application
    {
        private const string FILEPATH_ACTIVE_WIDGET_CONFIGURATION =
            "ActiveWidgetConfiguration.json";
        private const string FILEPATH_DEFAULT_WIDGET_CONFIGURATION =
            "DefaultWidgetConfiguration.json";
        private const string FILEPATH_FONT_WEIGHT_DICTIONARY = 
            "FontWeightDictionary.json";

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;

                bool hasDefaultWidgetConfigurationDeserialisedSuccessfully =
                    TryDeserialiseStartupPrerequisiteObjectFromFile<WidgetConfiguration>(
                            FILEPATH_DEFAULT_WIDGET_CONFIGURATION,
                            culture,
                            out WidgetConfiguration? defaultWidgetConfiguration,
                            out AlertWindow fatalStartupErrorDialog
                        );

                if (!hasDefaultWidgetConfigurationDeserialisedSuccessfully || 
                     defaultWidgetConfiguration == null)
                {
                    desktop.MainWindow = fatalStartupErrorDialog;
                    return;
                }

                bool isDefaultWidgetConfigurationValid =
                    IsDefaultWidgetConfigurationValid(
                        defaultWidgetConfiguration, culture, out fatalStartupErrorDialog);

                if (!isDefaultWidgetConfigurationValid)
                {
                    desktop.MainWindow = fatalStartupErrorDialog;
                    return;
                }

                bool hasFontWeightDictionaryDeserialisedSuccessfully =
                    TryDeserialiseStartupPrerequisiteObjectFromFile<Dictionary<string, FontWeight>>(
                            FILEPATH_FONT_WEIGHT_DICTIONARY,
                            culture,
                            out Dictionary<string, FontWeight>? fontWeightDictionary,
                            out fatalStartupErrorDialog
                        );

                if (!hasFontWeightDictionaryDeserialisedSuccessfully || 
                     fontWeightDictionary == null)
                {
                    desktop.MainWindow = fatalStartupErrorDialog;
                    return;
                }

                AutoSuspendHelper suspension = new(desktop);

                /* TODO:
                 * 
                 * This instance of AutoSuspendHelper needs to be disposed of at some point before 
                 * the application is exited; the compiler currently raises warning CA2000. I am 
                 * having some trouble addressing this. The AutoSuspendHelper instance needs to be 
                 * present in memory each time the application state is persisted to disk. 
                 * Subscribing to the IControlledApplicationLifetime.Exit event and disposing of the 
                 * object inside an event handler is a potential solution, but the compiler simply 
                 * raises warning CA1001 instead. 
                 * 
                 * Update, 2025-04-13. I have raised a discussion in the ReactiveUI GitHub 
                 * repository to address this. 
                 * 
                 * See: https://github.com/reactiveui/ReactiveUI/discussions/4012 */

                RxApp.SuspensionHost.CreateNewAppState = () =>
                {
                    /* Initialise the CreateNewAppState factory. If the app has no saved data, or 
                     * if the saved data is corrupt, ReactiveUI invokes this factory method to 
                     * create a default instance of the application state object. */

                    return defaultWidgetConfiguration;
                };

                RxApp.SuspensionHost.SetupDefaultSuspendResume(
                    new SuspensionDriver<WidgetConfiguration>(FILEPATH_ACTIVE_WIDGET_CONFIGURATION)
                );

                suspension.OnFrameworkInitializationCompleted();

                /* Restore saved widget settings if they exist. Otherwise, proceed using a default 
                 * configuration. */

                WidgetConfiguration userConfiguration =
                    RxApp.SuspensionHost.GetAppState<WidgetConfiguration>();

                List<FontFamily> availableFonts =
                    [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

                WidgetWindow view = new();
                WidgetModel model = new();

                WidgetViewModel viewModel =
                    new(model, 
                        availableFonts, 
                        fontWeightDictionary, 
                        userConfiguration, 
                        culture);

                view.DataContext = viewModel;
                desktop.MainWindow = view;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}