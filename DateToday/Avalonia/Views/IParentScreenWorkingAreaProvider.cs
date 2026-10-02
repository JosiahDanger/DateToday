using Avalonia;

namespace DateToday.Avalonia.Views;

internal interface IParentScreenWorkingAreaProvider
{
	Size GetParentScreenWorkingArea(Point enclosedPoint);
}
