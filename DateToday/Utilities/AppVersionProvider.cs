using DateToday.Avalonia.Resources;
using System;
using System.Diagnostics;
using System.IO;

namespace DateToday.Utilities;

internal static class AppVersionProvider
{
	public static string GetAppVersion()
	{
		string? executableFilepath = Environment.ProcessPath;

		if (string.IsNullOrEmpty(executableFilepath) || !File.Exists(executableFilepath))
		{
			throw new InvalidOperationException(
				Strings.AppVersionProvider_Exception_ExecutableFilepathNotAvailable);
		}

		string? appVersionString =
			FileVersionInfo.GetVersionInfo(executableFilepath).ProductVersion;

		if (string.IsNullOrEmpty(appVersionString))
		{
			throw new InvalidOperationException(
				Strings.AppVersionProvider_Exception_ExecutableVersionUnspecified);
		}

		return $"{Strings.AppVersionProvider_Prefix} {appVersionString}";
	}
}
