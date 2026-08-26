namespace DateToday.Avalonia.Messaging;

internal sealed class IsMouseDragEnabledChangedMessage(bool isMouseDragEnabled)
{
	public bool IsMouseDragEnabled { get; } = isMouseDragEnabled;
}
