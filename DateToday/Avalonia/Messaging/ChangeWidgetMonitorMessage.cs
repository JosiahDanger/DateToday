using Avalonia.Platform;

namespace DateToday.Avalonia.Messaging;

internal sealed class ChangeWidgetMonitorMessage(Screen newParentScreen)
{
	public Screen ParentScreen { get; } = newParentScreen;
}
