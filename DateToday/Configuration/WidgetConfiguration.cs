using Avalonia;

namespace DateToday.Configuration
{
    internal class WidgetConfiguration(
        PixelPoint windowPosition, string fontFamilyName, int fontSize, string fontWeightLookupKey,
        string dateFormat, byte? ordinalDaySuffixPosition)
    {
        /* This class constitutes a simple data structure into which persisted settings may be 
         * deserialised. */

        private readonly PixelPoint _windowPosition = windowPosition;
        private readonly string _fontFamilyName = fontFamilyName;
        private readonly int _fontSize = fontSize;
        private readonly string _fontWeightLookupKey = fontWeightLookupKey;
        private readonly string _dateFormat = dateFormat;
        private readonly byte? _ordinalDaySuffixPosition = ordinalDaySuffixPosition;

        public PixelPoint WindowPosition => _windowPosition;

        public string FontFamilyName => _fontFamilyName;

        public int FontSize => _fontSize;

        public string FontWeightLookupKey => _fontWeightLookupKey;

        public string DateFormat => _dateFormat;

        public byte? OrdinalDaySuffixPosition => _ordinalDaySuffixPosition;
    }
}
