using CommunityToolkit.Mvvm.Messaging;
using DateToday.Avalonia.Messaging;
using DateToday.Models;
using System;

namespace DateToday.Services;

internal sealed class WidgetModelMutationService
{
	private readonly WidgetModel _widgetModel;

	public WidgetModelMutationService(WidgetModel widgetModel)
	{
		_widgetModel = widgetModel;

		WeakReferenceMessenger.Default.Register<WidgetMonitorChangedMessage>(
			this,
			(_, message) =>
			{
				MutatePositionConfig(positionConfig =>
					positionConfig with { MonitorReference = message.MonitorReference });
			});

		WeakReferenceMessenger.Default.Register<ToggleMouseDragMessage>(
			this,
			(_, message) =>
			{
				MutatePositionConfig(positionConfig =>
					positionConfig with { IsMouseDragEnabled = message.IsMouseDragEnabled });
			});
	}

	public void MutateContentConfig(Func<ContentConfig, ContentConfig> mutator)
	{
		_widgetModel.Content = mutator(_widgetModel.Content);
	}

	public void MutatePositionConfig(Func<PositionConfig, PositionConfig> mutator)
	{
		_widgetModel.Position = mutator(_widgetModel.Position);
	}

	public void MutateFontConfig(Func<FontConfig, FontConfig> mutator)
	{
		_widgetModel.Font = mutator(_widgetModel.Font);
	}

	public void MutateAppConfig(Func<AppConfig, AppConfig> mutator)
	{
		_widgetModel.App = mutator(_widgetModel.App);
	}
}
