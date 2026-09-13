using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.ViewModels;
using System.Collections.Generic;

namespace DateToday.Avalonia.Views;

internal sealed partial class WidgetView : Window
{
	private readonly IWidgetViewDependencyProvider _dependencies;

	public WidgetView(IWidgetViewDependencyProvider dependencies)
	{
		InitializeComponent();

		_dependencies = dependencies;

		WeakReferenceMessenger.Default.Register<ToggleMouseDragMessage>(
			this, (_, message) => UpdateMouseDragToggleIcon(message.IsMouseDragEnabled));

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());

		UpdateMouseDragToggleIcon(_dependencies.Position.IsMouseDragEnabled);
	}

	private void UpdateMouseDragToggleIcon(bool isMouseDragEnabled)
	{
		string iconKey = isMouseDragEnabled ? "lock_regular" : "unlock_regular";

		if (this.FindResource(iconKey) is StreamGeometry icon)
		{
			IsMouseDragEnabledToggleIcon.Data = icon;
		}
		else
		{
			throw new KeyNotFoundException(
				Strings.WidgetView_Exception_FailedToLocateMouseDragToggleIcon);
		}
	}
}
