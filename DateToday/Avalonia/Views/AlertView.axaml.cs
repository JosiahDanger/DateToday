using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;

namespace DateToday.Avalonia.Views;

internal sealed partial class AlertView : Window
{
	public AlertView()
	{
		InitializeComponent();

		WeakReferenceMessenger.Default.Register<CloseAlertMessage>(
			this, (_, _) => this.Close());
	}
}
