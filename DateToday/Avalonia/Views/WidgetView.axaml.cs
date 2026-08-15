using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;

namespace DateToday.Avalonia.Views;

internal sealed partial class WidgetView : Window
{
	public WidgetView()
	{
		InitializeComponent();

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());
	}
}
