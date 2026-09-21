using Avalonia;

namespace DateToday.Avalonia.Messaging;

internal sealed class WidgetMonitorChangedMessage(
	string? monitorReference, Point anchoredCornerLogicalPosition)
{
	public string? MonitorReference { get; } = monitorReference;
	public Point AnchoredCornerLogicalPosition { get;  } = anchoredCornerLogicalPosition;
}
