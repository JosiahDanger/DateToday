using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DateToday.Views
{
    internal interface IWidgetView
    {
        PixelPoint WidgetPosition { get; }

        void CloseWidget(object? dialogResult);
    }

    internal partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetView
    {
        public WidgetWindow()
        {
            if (Design.IsDesignMode)
            {
                // Make the previewer happy.
                return;
            };

            InitializeComponent();
            this.WhenActivated(disposables => 
            {
                this.HandleActivation();

                Disposable
                    .Create(() => this.HandleDeactivation())
                    .DisposeWith(disposables);

                ViewModel.WhenAnyValue(x => x.PositionOAPH)
                         .ObserveOn(RxApp.MainThreadScheduler)
                         .BindTo(this, x => x.Position)
                         .DisposeWith(disposables);
            });
        }

        private void HandleActivation() 
        {
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowDialogAsync);
        }

        private void HandleDeactivation()
        {
            ViewModel!.Dispose();
        }

        public PixelPoint WidgetPosition => Position;

        public void CloseWidget(object? dialogResult)
        {
            Close(dialogResult);
        }

        private async Task DoShowDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this).ConfigureAwait(true);
            interaction.SetOutput(dialogResult);
        }
    }
}