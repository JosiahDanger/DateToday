using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DateToday.Drivers;
using DateToday.ViewModels;
using DateToday.Views;
using ReactiveUI;

namespace DateToday
{
    public partial class App : Application
    {
        private static WidgetWindow WidgetFactory(WidgetViewModel? inputViewModel)
        {
            WidgetViewModel viewModel;

            if (inputViewModel != null )
            {
                viewModel = inputViewModel;
            }
            else
            {
                viewModel = new();
            }

            WidgetWindow view = new() { DataContext = viewModel };
            viewModel.AttachViewInterface(view);
            
            return view;
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

                RxApp.SuspensionHost.CreateNewAppState = () =>
                {
                    /* Initialise the CreateNewAppState factory. If the app has no saved data, or 
                     * if the saved data is corrupt, ReactiveUI invokes this factory method to 
                     * create a default instance of the application state View Model object. */

                    return new WidgetViewModel();
                };

                RxApp.SuspensionHost.SetupDefaultSuspendResume(
                    new SuspensionDriver("AppState.json")
                );

                suspension.OnFrameworkInitializationCompleted();

                // Load the saved View Model state if it exists.
                WidgetViewModel? restoredViewModel = 
                    RxApp.SuspensionHost.GetAppState<WidgetViewModel>();

                desktop.MainWindow = WidgetFactory(restoredViewModel);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}