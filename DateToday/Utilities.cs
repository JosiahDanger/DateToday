using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Enums;
using DateToday.ViewModels;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace DateToday
{
    internal static class Utilities
    {
        public static AlertWindow AlertFactory(AlertType importance, string alertMessage)
        {
            AlertWindow view = new();
            AlertViewModel viewModel = new(view, importance, alertMessage);

            view.DataContext = viewModel;
            return view;
        }

        public static bool ReadTextFile(string filepath, out string text)
        {
            bool wasFileReadSuccessfully = false;

            try
            {
                // See: https://stackoverflow.com/a/67161275

                using FileStream? openedTextFile = 
                    // If this succeeds, the application has exclusive access to the target file.
                    File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.None);

                using StreamReader? textReader = new(openedTextFile);

                text = textReader.ReadToEnd();
                wasFileReadSuccessfully = true;
            }
            catch (Exception)
            {
                throw;
            }

            return wasFileReadSuccessfully;
        }

        public static T? DeserialiseFile<T>(string filepath)
        {
            try
            {
                bool wasFileReadSuccessfully = ReadTextFile(filepath, out string jsonBuffer);

                if (wasFileReadSuccessfully)
                {
                    return JsonConvert.DeserializeObject<T>(jsonBuffer);
                }
            }
            catch
            {
                throw;
            }

            return default;
        }

        public static Color InitialiseThemedColour(Window view, string resourceKey, Color fallback)
        {
            view.TryFindResource(
                resourceKey, view.ActualThemeVariant, out var themedColourResourceOrNull);

            if (themedColourResourceOrNull is Color themedColourResource)
            {
                return themedColourResource;
            }
            else
            {
                Debug.WriteLine(
                    $"Failed to discern thematically-appropriate colour associated with key: " +
                    $"'{resourceKey}'. Using {fallback} instead.");

                return fallback;
            }
        }

        public static IBrush InitialiseThemedBrush(Window view, string resourceKey, IBrush fallback)
        {
            view.TryFindResource(
                resourceKey, view.ActualThemeVariant, out var themedBrushResourceOrNull);

            if (themedBrushResourceOrNull is IBrush themedBrushResource)
            {
                return themedBrushResource;
            }
            else
            {
                Debug.WriteLine(
                    $"Failed to discern thematically-appropriate brush associated with key: " +
                    $"'{resourceKey}'. Using {fallback} instead.");

                return fallback;
            }
        }
    }
}
