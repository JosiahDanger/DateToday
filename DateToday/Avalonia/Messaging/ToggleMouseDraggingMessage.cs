namespace DateToday.Avalonia.Messaging;

internal sealed class ToggleMouseDraggingMessage(bool isMouseDraggingEnabled)
{
	public bool IsMouseDraggingEnabled { get; } = isMouseDraggingEnabled;
}
