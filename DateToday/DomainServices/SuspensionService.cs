using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.DomainServices;

/// <summary>
/// A service capable of loading and saving application state to persistent storage.
/// </summary>

internal static class SuspensionService
{
	private static readonly string s_roamingAppDataDirectoryPath =
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	private static readonly string s_dateTodayConfigDirectory =
		Path.Combine(
			s_roamingAppDataDirectoryPath,
			Strings.Suspension_DirectoryName_DateToday);

	private static readonly string s_widgetStateFilepath =
		Path.Combine(
			s_dateTodayConfigDirectory,
			Strings.Suspension_Filename_WidgetState);

	private static readonly CompositeFormat s_failedToDeserialiseStateExceptionFormat =
		CompositeFormat.Parse(
			Strings.Suspension_Exception_FailedToDeserialiseState_MessageTemplate);

	private static readonly CompositeFormat s_failedToPersistStateExceptionFormat =
		CompositeFormat.Parse(Strings.Suspension_Exception_FailedToPersistState_MessageTemplate);

	/// <summary>
	/// Saves an application state to persistent storage by means of synchronous operations.
	/// </summary>
	/// <typeparam name="T">The state type.</typeparam>
	/// <param name="state">The state to persist.</param>
	/// <param name="typeInfo">
	/// The source-generated metadata for <typeparamref name="T" />.
	/// </param>
	/// <exception cref="IOException">
	/// Thrown if the state cannot be persisted.
	/// </exception>

	public static void SaveState<T>(T state, JsonTypeInfo<T> typeInfo) where T : ISuspensionState
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(typeInfo);

		try
		{
			Directory.CreateDirectory(s_dateTodayConfigDirectory);

			using FileStream targetFileStream =
				File.Open(s_widgetStateFilepath, FileMode.Create, FileAccess.Write, FileShare.None);

			JsonSerializer.Serialize(targetFileStream, state, typeInfo);
		}
		catch (Exception ex)
		{
			string exceptionMessageFriendly =
				string.Format(
					CultureInfo.InvariantCulture,
					s_failedToPersistStateExceptionFormat,
					s_dateTodayConfigDirectory,
					ex.Message);

			throw new IOException(exceptionMessageFriendly, ex);
		}
	}

	/// <summary>
	/// Loads an application state from persistent storage by means of synchronous operations.
	/// </summary>
	/// <typeparam name="T">The expected state type.</typeparam>
	/// <param name="typeInfo">
	/// The source-generated metadata for <typeparamref name="T" />.
	/// </param>
	/// <returns>
	/// The deserialised application state, or <c>null</c> if no persisted state exists.
	/// </returns>
	/// <exception cref="IOException">
	/// Thrown if the persisted state exists but cannot be deserialised.
	/// </exception>

	public static T? LoadState<T>(JsonTypeInfo<T> typeInfo) where T : ISuspensionState
	{
		ArgumentNullException.ThrowIfNull(typeInfo);

		if (!File.Exists(s_widgetStateFilepath))
		{
			return default;
		}

		try
		{
			using FileStream targetFileStream = File.OpenRead(s_widgetStateFilepath);
			T? result = JsonSerializer.Deserialize(targetFileStream, typeInfo);

			return result;
		}
		catch (Exception ex)
		{
			string exceptionMessageFriendly =
				string.Format(
					CultureInfo.InvariantCulture,
					s_failedToDeserialiseStateExceptionFormat,
					s_dateTodayConfigDirectory,
					ex.Message);

			throw new IOException(exceptionMessageFriendly, ex);
		}
	}
}
