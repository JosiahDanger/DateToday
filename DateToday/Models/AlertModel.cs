namespace DateToday.Models;

internal sealed class AlertModel(AlertFlavour flavour, string message)
{
	public AlertFlavour Flavour { get; set; } = flavour;
	public string Message { get; set; } = message;
}
