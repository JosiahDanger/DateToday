using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Enums;
using DateToday.ViewModels;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace DateToday
{
    internal static class Utilities
    {
        private const string FATAL_ERROR_INVALID_JSON_FILE_INNER_MESSAGE_FILE_IS_EMPTY =
            "Is this your idea of a joke? This file is empty. You did this on purpose, didn't " +
            "you? You fucking cretin.";

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_UNHANDLED_FILE_EXCEPTION =
            CompositeFormat.Parse(

                // TODO: This probably isn't necessary?

                "An unexpected error occurred in attempting to parse file '{0}'. If you are " +
                "reading this message, then the application's error-handling functionality has " +
                "failed. Please raise an issue.");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_NOT_FOUND =
            CompositeFormat.Parse(
                "You absolute donkey. You've lost '{0}', haven't you? " +
                Environment.NewLine +
                "Go stand in the corner and think about what you've done. You fucking donut.");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_IO_EXCEPTION =
            CompositeFormat.Parse(
                "An I/O error occurred in trying to parse file '{0}': " +
                Environment.NewLine +
                Environment.NewLine +
                "{1}");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_FILE_ACCESS_DENIED =
            CompositeFormat.Parse(
                "The application is not permitted access to file '{0}': " +
                Environment.NewLine +
                Environment.NewLine +
                "{1}");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_JSON_EXCEPTION =
            CompositeFormat.Parse(
                "An error has occurred during deserialisation of file '{0}'. " +
                Environment.NewLine +
                Environment.NewLine +
                "Exception message: " +
                Environment.NewLine +
                Environment.NewLine +
                "{1}");

        private static readonly CompositeFormat FATAL_ERROR_MESSAGE_DEFAULT_CONFIGURATION_INVALID =
            CompositeFormat.Parse(
                "Default widget configuration is invalid: " +
                Environment.NewLine +
                Environment.NewLine +
                "{0}");

        private static readonly CompositeFormat
            FATAL_ERROR_DEFAULT_CONFIGURATION_INVALID_INNER_MESSAGE_FONT_SIZE =
                CompositeFormat.Parse("FontSize must be greater than {0}.");

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

                using FileStream openedTextFile = 
                    // If this succeeds, the application has exclusive access to the target file.
                    File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.None);

                using StreamReader textReader = new(openedTextFile);

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

        public static bool TryDeserialiseStartupPrerequisiteObjectFromFile<T>(
            string filepath, CultureInfo culture,
            out T? deserialisedObject, out AlertWindow errorDialog)
        {
            deserialisedObject = default;
            bool hasFileDeserialisedSuccessfully = false;

            string deserialisationErrorMessage =
                string.Format(
                    culture,
                    FATAL_ERROR_MESSAGE_UNHANDLED_FILE_EXCEPTION,
                    filepath);

            try
            {
                deserialisedObject =
                    DeserialiseFile<T>(filepath) ??
                    throw new Newtonsoft.Json.JsonSerializationException(
                        FATAL_ERROR_INVALID_JSON_FILE_INNER_MESSAGE_FILE_IS_EMPTY);

                hasFileDeserialisedSuccessfully = true;
            }
            catch (FileNotFoundException)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_NOT_FOUND,
                        filepath);
            }
            catch (IOException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_IO_EXCEPTION,
                        filepath,
                        e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_FILE_ACCESS_DENIED,
                        filepath,
                        e.Message);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_JSON_EXCEPTION,
                        filepath,
                        e.Message);
            }

            errorDialog = AlertFactory(AlertType.FatalError, deserialisationErrorMessage);

            return hasFileDeserialisedSuccessfully;
        }

        public static bool IsDefaultWidgetConfigurationValid(
            WidgetConfiguration configuration, CultureInfo culture, out AlertWindow errorDialog)
        {
            const byte FONT_SIZE_MIN = 10;

            bool isValid = true;
            string widgetConfigurationValidationErrorMessage = string.Empty;

            if (configuration.FontSize < FONT_SIZE_MIN)
            {
                isValid = false;

                string invalidFontSizeErrorInnerMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_DEFAULT_CONFIGURATION_INVALID_INNER_MESSAGE_FONT_SIZE,
                        FONT_SIZE_MIN);

                widgetConfigurationValidationErrorMessage =
                    string.Format(
                        culture,
                        FATAL_ERROR_MESSAGE_DEFAULT_CONFIGURATION_INVALID,
                        invalidFontSizeErrorInnerMessage);
            }

            errorDialog =
                AlertFactory(AlertType.FatalError, widgetConfigurationValidationErrorMessage);

            return isValid;
        }

        public static Color InitialiseThemedColour(
            StyledElement element, string resourceKey, Color fallback)
        {
            element.TryFindResource(
                resourceKey, element.ActualThemeVariant, out var themedColourResourceOrNull);

            if (themedColourResourceOrNull is Color themedColourResource)
            {
                return themedColourResource;
            }
            else
            {
                // TODO: Make this a warning alert dialog.

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
                // TODO: Make this a warning alert dialog.

                Debug.WriteLine(
                    $"Failed to discern thematically-appropriate brush associated with key: " +
                    $"'{resourceKey}'. Using {fallback} instead.");

                return fallback;
            }
        }
    }
}
