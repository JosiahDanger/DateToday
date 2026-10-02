using DateToday.Models;
using System;

namespace DateToday.DomainServices;

internal sealed class WidgetModelMutationService(WidgetModel widgetModel)
{
	private readonly WidgetModel _widgetModel = widgetModel;

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
