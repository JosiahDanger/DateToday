using CommunityToolkit.Mvvm.ComponentModel;

namespace DateToday.Models;

internal sealed partial class WidgetModel(
	ContentConfig content,
	PositionConfig position,
	FontConfig font,
	AppConfig app) : ObservableObject
{
	[ObservableProperty]
	public partial ContentConfig Content { get; set; } = content;

	[ObservableProperty]
	public partial PositionConfig Position { get; set; } = position;

	[ObservableProperty]
	public partial FontConfig Font { get; set; } = font;

	[ObservableProperty]
	public partial AppConfig App { get; set; } = app;
}
