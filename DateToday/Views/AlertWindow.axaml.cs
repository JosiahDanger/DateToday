using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DateToday.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DateToday;

internal partial class AlertWindow : ReactiveWindow<AlertViewModel>
{
    public AlertWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Make the previewer happy.
            return;
        }

        this.WhenActivated(disposables =>
        {
            ViewModel!.CloseAlert
                      .ObserveOn(RxApp.MainThreadScheduler)
                      .Subscribe(_ => Close())
                      .DisposeWith(disposables);
        });
    }
}