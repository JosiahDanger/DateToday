namespace DateToday.Avalonia.Messaging;

internal sealed class WidgetMonitorChangedMessage(string monitorReference)
{
	public string MonitorReference { get; } = monitorReference;
}
