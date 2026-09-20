using Avalonia;

namespace DateToday.Avalonia.Messaging;

internal sealed class WidgetDraggedMessage(Point anchoredCornerScaledPosition)
{
	public Point AnchoredCornerScaledPosition { get; } = anchoredCornerScaledPosition;
}
