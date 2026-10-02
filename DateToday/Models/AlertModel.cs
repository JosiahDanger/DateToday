namespace DateToday.Models;

internal sealed class AlertModel(AlertFlavour flavour, string message)
{
	public AlertFlavour Flavour { get; private set; } = flavour;
	public string Message { get; private set; } = message;
}
