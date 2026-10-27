using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DateToday.Avalonia.ViewModels;
using DateToday.Avalonia.Views;
using System;

namespace DateToday.Avalonia.DataTemplates;

internal class SettingsDataTemplate : IDataTemplate
{
	public Control Build(object? data) =>
		data switch
		{
			ContentConfigViewModel => new ContentConfigView(),
			//PositionConfigViewModel => new PositionConfigView(),
			//FontConfigViewModel => new FontConfigView(),
			//AppConfigViewModel => new AppConfigView(),
			_ => throw new ArgumentOutOfRangeException(nameof(data))
		};

	public bool Match(object? data) =>
		data is ContentConfigViewModel
			//or PositionConfigViewModel
			//or FontConfigViewModel
			//or AppConfigViewModel
			;
}
