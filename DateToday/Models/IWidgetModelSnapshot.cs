using System.ComponentModel;

namespace DateToday.Models;

internal interface IWidgetModelSnapshot : INotifyPropertyChanged
{
	PositionConfig Position { get; }
	ContentConfig Content { get; }
	FontConfig Font { get; }
	AppConfig App { get; }
}
