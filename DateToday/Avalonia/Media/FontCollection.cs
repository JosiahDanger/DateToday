using Avalonia.Media.Fonts;
using System;

namespace DateToday.Avalonia.Media;

internal sealed class FontCollection : EmbeddedFontCollection
{
	public FontCollection() : base(
		new Uri("fonts:FontCollection", UriKind.Absolute),
		new Uri("avares://DateToday/Avalonia/Fonts", UriKind.Absolute))
	{
	}
}
