using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;

namespace DateToday.Avalonia.Views;

internal sealed partial class SettingsView : Window
{
	public SettingsView()
	{
		InitializeComponent();

		WeakReferenceMessenger.Default.Register<CloseSettingsViewMessage>(
			this, (_, _) => this.Close());
	}
}
