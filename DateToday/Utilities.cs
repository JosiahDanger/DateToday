using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DateToday.Configuration;
using DateToday.Enums;
using DateToday.Resources;
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
        private static readonly CompositeFormat BASE_ERROR_MESSAGE_DESERIALISATION_EXCEPTION =
            CompositeFormat.Parse(BaseErrorMessageFormats.DeserialisationException);

        private static readonly CompositeFormat ERROR_MESSAGE_DESERIALISED_FILE_IS_EMPTY =
            CompositeFormat.Parse(DeserialisationErrorMessageFormats.FileIsEmpty);

        private static readonly CompositeFormat ERROR_MESSAGE_FILE_NOT_FOUND =
            CompositeFormat.Parse(DeserialisationErrorMessageFormats.FileNotFound);

        private static readonly CompositeFormat ERROR_MESSAGE_FILE_IO_EXCEPTION =
            CompositeFormat.Parse(DeserialisationErrorMessageFormats.InputOutputException);

        private static readonly CompositeFormat ERROR_MESSAGE_FILE_ACCESS_DENIED =
            CompositeFormat.Parse(DeserialisationErrorMessageFormats.AccessDenied);

        private static readonly CompositeFormat BASE_ERROR_MESSAGE_DEFAULT_CONFIGURATION_INVALID =
            CompositeFormat.Parse(BaseErrorMessageFormats.DefaultConfigurationInvalid);

        private static readonly CompositeFormat ERROR_MESSAGE_FONT_SIZE_INVALID =
            CompositeFormat.Parse(
                DefaultConfigurationValidationErrorMessageFormats.FontSizeInvalid);

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
            string deserialisationErrorMessage = string.Empty;

            try
            {
                deserialisedObject = DeserialiseFile<T>(filepath);

                if (deserialisedObject == null)
                {
                    deserialisationErrorMessage =
                        string.Format(culture, ERROR_MESSAGE_DESERIALISED_FILE_IS_EMPTY, filepath);

                    throw new Newtonsoft.Json.JsonSerializationException(
                        deserialisationErrorMessage);
                }

                hasFileDeserialisedSuccessfully = true;
            }
            catch (FileNotFoundException)
            {
                deserialisationErrorMessage =
                    string.Format(culture, ERROR_MESSAGE_FILE_NOT_FOUND, filepath);
            }
            catch (IOException e)
            {
                deserialisationErrorMessage =
                    string.Format(culture, ERROR_MESSAGE_FILE_IO_EXCEPTION, filepath, e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                deserialisationErrorMessage =
                    string.Format(culture, ERROR_MESSAGE_FILE_ACCESS_DENIED, filepath, e.Message);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                deserialisationErrorMessage =
                    string.Format(
                        culture, BASE_ERROR_MESSAGE_DESERIALISATION_EXCEPTION, filepath, e.Message);
            }

            errorDialog = AlertFactory(AlertType.FatalError, deserialisationErrorMessage);

            return hasFileDeserialisedSuccessfully;
        }

        public static bool IsDefaultWidgetConfigurationValid(
            WidgetConfiguration configuration, CultureInfo culture, out AlertWindow errorDialog)
        {
            const byte FONT_SIZE_MIN = 10;

            bool isValid = true;
            string configurationValidationErrorInnerMessage = string.Empty;

            if (configuration.FontSize < FONT_SIZE_MIN)
            {
                isValid = false;

                configurationValidationErrorInnerMessage =
                    string.Format(culture, ERROR_MESSAGE_FONT_SIZE_INVALID, FONT_SIZE_MIN);
            }

            string configurationValidationErrorMessage =
                string.Format(
                    culture, 
                    BASE_ERROR_MESSAGE_DEFAULT_CONFIGURATION_INVALID, 
                    configurationValidationErrorInnerMessage);

            errorDialog =
                AlertFactory(AlertType.FatalError, configurationValidationErrorMessage);

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
