using Avalonia;

namespace DateToday.Avalonia.Messaging;

internal sealed class WidgetPositionUpdatedMessage(Point anchoredCornerNewLogicalPosition)
{
	public Point AnchoredCornerLogicalPosition { get; } = anchoredCornerNewLogicalPosition;
}
