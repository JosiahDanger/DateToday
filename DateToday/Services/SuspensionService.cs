using DateToday.Avalonia.Resources;
using DateToday.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DateToday.Services;

/// <summary>
/// A service capable of loading and saving application state to persistent storage.
/// </summary>

internal static class SuspensionService
{
	private static readonly string _filepath =
		Path.Combine(AppContext.BaseDirectory, Strings.Suspension_Filename_WidgetState);

	/// <summary>
	/// Save an application state to persistent storage by means of synchronous operations.
	/// </summary>
	/// <typeparam name="T">The state type.</typeparam>
	/// <param name="state">The state to persist.</param>
	/// <param name="typeInfo">
	/// The source-generated metadata for <typeparamref name="T"/>.
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the state cannot be persisted.
	/// </exception>

	public static void SaveState<T>(T state, JsonTypeInfo<T> typeInfo) where T : ISuspensionState
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(typeInfo);

		string? directoryPath = Path.GetDirectoryName(_filepath);

		if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		try
		{
			using FileStream targetFileStream =
				File.Open(_filepath, FileMode.Create, FileAccess.Write, FileShare.None);

			JsonSerializer.Serialize(targetFileStream, state, typeInfo);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				Strings.Suspension_Exception_FailedToPersistState,
				ex);
		}
	}

	/// <summary>
	/// Loads an application state from persistent storage by means of synchronous operations.
	/// </summary>
	/// <typeparam name="T">The expected state type.</typeparam>
	/// <param name="typeInfo">
	/// The source-generated metadata for <typeparamref name="T"/>.
	/// </param>
	/// <returns>
	/// The deserialized application state, or <c>null</c> if no persisted state exists.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the persisted state cannot be deserialized.
	/// </exception>

	public static T? LoadState<T>(JsonTypeInfo<T> typeInfo) where T : ISuspensionState
	{
		ArgumentNullException.ThrowIfNull(typeInfo);

		if (!File.Exists(_filepath))
		{
			return default;
		}

		try
		{
			using FileStream targetFileStream = File.OpenRead(_filepath);
			T? result = JsonSerializer.Deserialize(targetFileStream, typeInfo);

			return result;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				Strings.Suspension_Exception_FailedToDeserialiseState,
				ex);
		}
	}
}
