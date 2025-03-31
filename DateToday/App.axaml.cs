using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using DateToday.Configuration;
using DateToday.Drivers;
using DateToday.Models;
using DateToday.ViewModels;
using DateToday.Views;
using Newtonsoft.Json;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;

namespace DateToday
{
    internal partial class App : Application
    {
        private const string FILEPATH_DEFAULT_WIDGET_CONFIGURATION =
            "DefaultWidgetConfiguration.json";
        private const string FILEPATH_FONT_WEIGHT_DICTIONARY = 
            "FontWeightDictionary.json";

        private static WidgetWindow WidgetFactory(WidgetConfiguration? restoredSettings)
        {
            WidgetWindow view = new();
            WidgetViewModel viewModel;
            WidgetModel model = new();

            Dictionary<string, FontWeight> fontWeightDictionary = 
                GetDeserialisedFontWeightDictionary(FILEPATH_FONT_WEIGHT_DICTIONARY);

            if (restoredSettings != null)
            {
                viewModel = new(view, model, fontWeightDictionary, restoredSettings);
            }
            else
            {
                WidgetConfiguration defaultSettings = 
                    GetDeserialisedWidgetConfiguration(FILEPATH_DEFAULT_WIDGET_CONFIGURATION);
                viewModel = new(view, model, fontWeightDictionary, defaultSettings);
            }

            view.DataContext = viewModel;
            return view;
        }

        private static WidgetConfiguration GetDeserialisedWidgetConfiguration(string filepath)
        {
            string jsonBuffer = File.ReadAllText(filepath);

            // TODO: Error handling.
            return JsonConvert.DeserializeObject<WidgetConfiguration>(jsonBuffer)!;
        }

        private static Dictionary<string, FontWeight> GetDeserialisedFontWeightDictionary(
            string filepath)
        {
            string jsonBuffer = File.ReadAllText(filepath);

            Dictionary<string, FontWeight>? dictionaryBuffer =
                JsonConvert.DeserializeObject<Dictionary<string, FontWeight>>(jsonBuffer);

            if (dictionaryBuffer != null)
            {
                return dictionaryBuffer;
            }
            else
            {
                // TODO: Error handling.
                return [];
            }
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                AutoSuspendHelper suspension = new(desktop);

                /* TODO:
                 * 
                 * This instance of AutoSuspendHelper needs to be disposed of at some point before
                 * the application is exited; the compiler currently raises warning #CA2000. I am
                 * having some trouble addressing this. The AutoSuspendHelper instance needs to be
                 * present in memory each time the application state is persisted to disk.
                 * Subscribing to the IControlledApplicationLifetime.Exit event and disposing of the
                 * object inside an event handler unfortunately does not resolve the warning. Might
                 * need to seek help from the Avalonia / ReactiveUI community. */

                RxApp.SuspensionHost.CreateNewAppState = () =>
                {
                    /* Initialise the CreateNewAppState factory. If the app has no saved data, or 
                     * if the saved data is corrupt, ReactiveUI invokes this factory method to 
                     * create a default instance of the application state object. */

                    return
                        GetDeserialisedWidgetConfiguration(FILEPATH_DEFAULT_WIDGET_CONFIGURATION);
                };

                RxApp.SuspensionHost.SetupDefaultSuspendResume(
                    new SuspensionDriver("AppState.json", typeof(WidgetConfiguration))
                );

                suspension.OnFrameworkInitializationCompleted();

                // Load saved widget settings should they exist.
                WidgetConfiguration? restoredSettings =
                    RxApp.SuspensionHost.GetAppState<WidgetConfiguration?>();

                desktop.MainWindow = WidgetFactory(restoredSettings);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}