using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Avalonia.Resources;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DateToday.Avalonia.PresentationServices;

internal sealed class GeometryProvider(IResourceHost application)
{
	private static readonly CompositeFormat s_geometryNotFoundFormat =
		CompositeFormat.Parse(Strings.GeometryProvider_Exception_FailedToLocateRequestedGeometry);

	private readonly IResourceHost _application = application;

	public StreamGeometry GetGeometry(string iconKey)
	{
		if (_application.FindResource(iconKey) is not StreamGeometry icon)
		{
			string geometryNotFoundExceptionMessage =
				string.Format(CultureInfo.InvariantCulture, s_geometryNotFoundFormat, iconKey);

			throw new KeyNotFoundException(geometryNotFoundExceptionMessage);
		}

		return icon;
	}
}
