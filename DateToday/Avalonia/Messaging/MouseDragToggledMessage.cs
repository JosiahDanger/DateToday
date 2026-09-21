using Avalonia;

namespace DateToday.Avalonia.Messaging;

internal sealed class WidgetDraggedMessage(Point anchoredCornerLogicalPosition)
{
	public Point AnchoredCornerLogicalPosition { get; } = anchoredCornerLogicalPosition;
}
