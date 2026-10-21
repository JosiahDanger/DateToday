using DateToday.DomainServices;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Models;

internal static class WidgetModelFactory
{
	/// <summary>
	/// Should a persisted <see cref="WidgetModelDto" /> already exist on disk, this method will
	/// attempt to deserialise it into a <see cref="WidgetModel" />.
	/// </summary>
	/// <returns>
	/// An instantiated WidgetModel, or <c>null</c>.
	/// </returns>
	/// <exception cref="System.IO.IOException">
	/// Thrown if the persisted state exists but cannot be deserialised.
	/// </exception>

	public static WidgetModel? GetPersistedWidgetModel()
	{
		JsonTypeInfo<WidgetModelDto> typeInfo =
			WidgetModelDtoSerialiserContext.Default.WidgetModelDto;

		// This might raise an IOException.
		WidgetModelDto? restoredStateDtoOrNull =
			SuspensionService.LoadState<WidgetModelDto>(typeInfo);

		return restoredStateDtoOrNull?.ToModel();
	}

	public static WidgetModel CreateDefault() =>
		new(
			WidgetModelDefaults.DefaultContentConfig,
			WidgetModelDefaults.DefaultPositionConfig,
			WidgetModelDefaults.DefaultFontConfig,
			WidgetModelDefaults.DefaultAppConfig);
}
