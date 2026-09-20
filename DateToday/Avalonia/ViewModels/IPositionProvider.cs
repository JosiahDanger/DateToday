using DateToday.Models;
using System.ComponentModel;

namespace DateToday.Avalonia.ViewModels;

internal interface IPositionProvider : INotifyPropertyChanged
{
	PositionConfig Position { get; }
}
