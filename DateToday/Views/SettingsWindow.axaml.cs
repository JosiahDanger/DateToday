using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace DateToday.Views;
public partial class SettingsWindow : ReactiveWindow<SettingsViewModel>
{
    public SettingsWindow()
    {
        InitializeComponent();

        this.WhenActivated(action => action(
            ViewModel!.CommandCloseSettingsView.Subscribe(dialogResult => Close(dialogResult))
        ));
    }
}