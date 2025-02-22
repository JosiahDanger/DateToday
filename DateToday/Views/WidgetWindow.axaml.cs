using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;

namespace DateToday.Views
{
    public interface IWidgetView
    {
        PixelPoint WidgetPosition { get; set; }
    }

    public partial class WidgetWindow : ReactiveWindow<WidgetViewModel>, IWidgetView
    {
        public WidgetWindow()
        {
            if (Design.IsDesignMode)
            {
                // Make the previewer happy.
                return;
            };

            InitializeComponent();
            this.WhenActivated(disposables => HandleActivation());
        }

        private void HandleActivation() 
        {
            ViewModel!.InteractionReceiveNewSettings.RegisterHandler(DoShowDialogAsync);
        }

        public PixelPoint WidgetPosition
        {
            get => Position;
            set => Position = value;
        }

        private async Task DoShowDialogAsync(
            IInteractionContext<SettingsViewModel, bool> interaction)
        {
            SettingsWindow dialog = new() { DataContext = interaction.Input };

            bool dialogResult = await dialog.ShowDialog<bool>(this);
            interaction.SetOutput(dialogResult);
        }
    }
}