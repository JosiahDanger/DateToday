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
using System.Linq;

namespace DateToday
{
    internal sealed partial class App : Application
    {
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
            static WidgetWindow WidgetFactory(WidgetConfiguration? restoredSettings)
            {
                WidgetWindow view = new();
                WidgetViewModel viewModel;
                WidgetModel model = new();

                List<FontFamily> availableFonts =
                    [.. FontManager.Current.SystemFonts.OrderBy(x => x.Name)];

                Dictionary<string, FontWeight> fontWeightDictionary =
                    GetDeserialisedFontWeightDictionary(FILEPATH_FONT_WEIGHT_DICTIONARY);

                if (restoredSettings != null)
                {
                    viewModel =
                        new(view, model, availableFonts, fontWeightDictionary, restoredSettings);
                }
                else
                {
                    WidgetConfiguration defaultSettings =
                        GetDeserialisedWidgetConfiguration(FILEPATH_DEFAULT_WIDGET_CONFIGURATION);
                    viewModel = 
                        new(view, model, availableFonts, fontWeightDictionary, defaultSettings);
                }

                view.DataContext = viewModel;
                return view;
            }

            static WidgetConfiguration GetDeserialisedWidgetConfiguration(string filepath)
            {
                string jsonBuffer = File.ReadAllText(filepath);

                // TODO: Error handling. Must remove null-forgiving operator later.
                return JsonConvert.DeserializeObject<WidgetConfiguration>(jsonBuffer)!;
            }

            static Dictionary<string, FontWeight> GetDeserialisedFontWeightDictionary(
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

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
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