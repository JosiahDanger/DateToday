using CommunityToolkit.Mvvm.ComponentModel;

namespace DateToday.Models;

/// <summary>
/// WidgetModel is a singleton Model layer responsible for mutation of the desktop widget.
/// Properties are mutable only through <see cref="WidgetModelMutationService"/>;
/// <see cref="IWidgetModelSnapshot"/> exposes them for read-only access by the
/// <see cref="WidgetViewModel"/>.
/// </summary>

internal sealed partial class WidgetModel(
	ContentConfig content,
	PositionConfig position,
	FontConfig font,
	AppConfig app) : ObservableObject, IWidgetModelSnapshot
{
	[ObservableProperty]
	public partial ContentConfig Content { get; internal set; } = content;

	[ObservableProperty]
	public partial PositionConfig Position { get; internal set; } = position;

	[ObservableProperty]
	public partial FontConfig Font { get; internal set; } = font;

	[ObservableProperty]
	public partial AppConfig App { get; internal set; } = app;
}
