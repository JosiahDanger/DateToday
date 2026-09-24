namespace DateToday.Avalonia.Messaging;

internal sealed class MouseDragToggledMessage(bool isMouseDragEnabled)
{
	public bool IsMouseDragEnabled { get; } = isMouseDragEnabled;
}
