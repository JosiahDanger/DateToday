using Avalonia;

namespace DateToday.Avalonia.Messaging;

internal sealed class ParentMonitorChangedMessage(
	string? monitorReference, Point anchoredCornerScaledPosition)
{
	public string? MonitorReference { get; } = monitorReference;
	public Point AnchoredCornerScaledPosition { get;  } = anchoredCornerScaledPosition;
}
