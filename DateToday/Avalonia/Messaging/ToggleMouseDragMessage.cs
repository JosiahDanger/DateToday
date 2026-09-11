namespace DateToday.Avalonia.Messaging;

internal sealed class ToggleMouseDragMessage(bool isMouseDragEnabled)
{
	public bool IsMouseDragEnabled { get; } = isMouseDragEnabled;
}
