using DateToday.Services;
using System;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Models;

internal static class WidgetModelFactory
{
	public static (WidgetModel initialWidgetModel, bool hasDeserialisationSucceeded)
		GetInitialWidgetModel()
	{
		bool hasDeserialisationSucceeded = TryRestoreState(out WidgetModel? restoredState);
		WidgetModel wm = restoredState ?? CreateDefault();

		return (wm, hasDeserialisationSucceeded);
	}

	/// <summary>
	/// Should a persisted <see cref="WidgetModelDto"/> object already exist on disk, this method
	/// will attempt to deserialise it into a <see cref="WidgetModel"/>.
	/// </summary>
	/// <param name="restoredState">
	/// This parameter is assigned a <see cref="WidgetModel"/> upon successful deserialisation. In
	/// the event that no persisted state exists, or a deserialisation error occurs, this parameter
	/// is assigned a value of <c>null</c>.
	/// </param>
	/// <returns>
	/// <c>true</c> if restoration succeeded or no persisted state exists; <c>false</c> if a
	/// deserialisation error occurred.
	/// </returns>

	private static bool TryRestoreState(out WidgetModel? restoredState)
	{
		restoredState = null;

		JsonTypeInfo<WidgetModelDto> typeInfo =
			WidgetModelDtoSerialiserContext.Default.WidgetModelDto;

		try
		{
			WidgetModelDto? restoredStateDtoOrNull =
				SuspensionService.LoadState<WidgetModelDto>(typeInfo);

			if (restoredStateDtoOrNull is WidgetModelDto restoredStateDto)
			{
				restoredState = restoredStateDto.ToModel();
			}
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		return true;
	}

	private static WidgetModel CreateDefault() =>
		new(
			WidgetModelDefaults.DefaultContentConfig,
			WidgetModelDefaults.DefaultPositionConfig,
			WidgetModelDefaults.DefaultFontConfig,
			WidgetModelDefaults.DefaultAppConfig);
}
