using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Avalonia.Resources;
using DateToday.ViewModels;
using System;
using System.Collections.Generic;

namespace DateToday.Avalonia.Views;

internal sealed partial class WidgetView : Window
{
	public WidgetView(IWidgetViewDependencyProvider dependencies)
	{
		InitializeComponent();

		this.Closed += OnClosed;

		WeakReferenceMessenger.Default.Register<ToggleMouseDragMessage>(
			this, (_, message) => HandleMouseDragToggle(message.IsMouseDragEnabled));

		WeakReferenceMessenger.Default.Register<CloseApplicationMessage>(
			this, (_, _) => this.Close());

		HandleMouseDragToggle(dependencies.Position.IsMouseDragEnabled);
	}

	private void UpdateMouseDragToggleIcon(bool isMouseDragEnabled)
	{
		string iconKey = isMouseDragEnabled ? "lock_regular" : "unlock_regular";

		if (this.FindResource(iconKey) is not StreamGeometry icon)
		{
			throw new KeyNotFoundException(
				Strings.WidgetView_Exception_FailedToLocateMouseDragToggleIcon);
		}

		IsMouseDragEnabledToggleIcon.Data = icon;
	}

	private void HandleMouseDragToggle(bool isMouseDragEnabled)
	{
		WindowDecorationsElementRole newWindowElementRole =
			isMouseDragEnabled
			? WindowDecorationsElementRole.TitleBar
			: WindowDecorationsElementRole.None;

		WindowDecorationProperties.SetElementRole(this, newWindowElementRole);

		UpdateMouseDragToggleIcon(isMouseDragEnabled);
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		WeakReferenceMessenger.Default.UnregisterAll(this);
	}
}
