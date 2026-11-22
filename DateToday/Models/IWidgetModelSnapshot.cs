using System.ComponentModel;

namespace DateToday.Models;

internal interface IWidgetModelSnapshot : INotifyPropertyChanged
{
	string DateTimeText { get; }

	ContentConfig Content { get; }
	PositionConfig Position { get; }
	FontConfig Font { get; }
	AppConfig App { get; }
}
