using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace DateToday.Views
{
    internal interface IWidgetView
    {
        PixelPoint WidgetPosition { get; set; }

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

        public PixelPoint WidgetPosition
        {
            get => Position;
            set => Position = value;
        }

        public void CloseWidget(object? dialogResult)
        {
            this.Close(dialogResult);
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