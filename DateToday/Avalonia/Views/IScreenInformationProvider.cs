using Avalonia;
using Avalonia.Platform;

namespace DateToday.Avalonia.Views;

internal interface IScreenInformationProvider
{
	Screen GetScreenEnclosingLogicalCoordinate(Point logicalCoordinate);
}
