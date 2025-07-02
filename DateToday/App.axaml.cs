using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using DateToday.Configuration;
using DateToday.Drivers;
using DateToday.Enums;
using DateToday.Models;
using DateToday.ViewModels;
using DateToday.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

        private static readonly string NEW_LINE = Environment.NewLine;
        private static readonly string NEW_PARAGRAPH = NEW_LINE + NEW_LINE;

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_UNHANDLED_FILE_EXCEPTION =
            CompositeFormat.Parse(
                "An unhandled exception occurred in attempting to parse file '{0}'.");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_NOT_FOUND = 
            CompositeFormat.Parse(
                "You absolute donkey. You've lost '{0}', haven't you? " + 
                NEW_LINE + 
                "Go stand in the corner and think about what you've done. You fucking donut.");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_IO =
            CompositeFormat.Parse(
                "An I/O error occurred while trying to parse file '{0}': " + 
                NEW_PARAGRAPH + 
                "{1}");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_ACCESS_DENIED =
            CompositeFormat.Parse(
                "The application is not permitted access to file '{0}': " + 
                NEW_PARAGRAPH + 
                "{1}");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_CONTENT_IS_NULL =
            CompositeFormat.Parse(
                "It would appear that the contents of file '{0}' is null: " + 
                NEW_PARAGRAPH + 
                "{1}");

        private static bool TryDeserialisePrerequisiteObjectFromFile<T>(
            string filepath, CultureInfo culture, 
            out T deserialisedObject, out AlertWindow errorDialog)
        {
            deserialisedObject = default!;

            bool hasFileDeserialisedSuccessfully = false;

            string deserialisationErrorMessage =
                string.Format(
                    culture,
                    FATAL_ERROR_MESSAGE_UNHANDLED_FILE_EXCEPTION, 
                    FILEPATH_DEFAULT_WIDGET_CONFIGURATION);

            try
            {
                T? deserialisedObjectOrNull = 
                    DeserialiseFile<T>(filepath) ?? 
                    throw new ArgumentNullException(paramName: filepath);

                deserialisedObject = deserialisedObjectOrNull;

                hasFileDeserialisedSuccessfully = true;
            }
            catch (FileNotFoundException)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_NOT_FOUND,
                        filepath);
            }
            catch (IOException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_IO,
                        filepath,
                        e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_ACCESS_DENIED,
                        filepath,
                        e.Message);
            }
            catch (ArgumentNullException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_CONTENT_IS_NULL,
                        filepath,
                        e.Message);
            }

            errorDialog = AlertFactory(AlertType.FatalError, deserialisationErrorMessage);

            return hasFileDeserialisedSuccessfully;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;

                bool isDefaultWidgetConfigurationDeserialisedSuccessfully =
                    TryDeserialisePrerequisiteObjectFromFile<WidgetConfiguration>(
                            FILEPATH_DEFAULT_WIDGET_CONFIGURATION,
                            culture,
                            out WidgetConfiguration defaultWidgetConfiguration,
                            out AlertWindow defaultWidgetConfigurationDeserialisationError
                        );

                if (!isDefaultWidgetConfigurationDeserialisedSuccessfully)
                {
                    desktop.MainWindow = defaultWidgetConfigurationDeserialisationError;
                    return;
                }

                bool isFontWeightDictionaryDeserialisedSuccessfully =
                    TryDeserialisePrerequisiteObjectFromFile<Dictionary<string, FontWeight>>(
                            FILEPATH_FONT_WEIGHT_DICTIONARY,
                            culture,
                            out Dictionary<string, FontWeight> fontWeightDictionary,
                            out AlertWindow fontWeightDictionaryDeserialisationError
                        );

                if (!isFontWeightDictionaryDeserialisedSuccessfully)
                {
                    desktop.MainWindow = fontWeightDictionaryDeserialisationError;
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
                    new(view, model, availableFonts, fontWeightDictionary, userConfiguration, 
                        culture);

                view.DataContext = viewModel;

                desktop.MainWindow = view;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}