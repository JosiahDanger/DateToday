using Avalonia;
using Avalonia.Media;
using DateToday.Enums;

namespace DateToday.Configuration
{
    internal sealed class WidgetConfiguration(
        Point anchoredCornerScaledPosition, WindowVertexIdentifier anchoredCorner, 
        string fontFamilyName, int fontSize, string fontWeightLookupKey, Color? customFontColour, 
        bool isDropShadowEnabled, Color? customDropShadowColour, string dateFormat, 
        byte? ordinalDaySuffixPosition)
    {
        /* This class constitutes a simple data structure into which persisted settings may be 
         * deserialised. */

        private readonly Point _anchoredCornerScaledPosition = anchoredCornerScaledPosition;
        private readonly WindowVertexIdentifier _anchoredCorner = anchoredCorner;
        private readonly string _fontFamilyName = fontFamilyName;
        private readonly int _fontSize = fontSize;
        private readonly string _fontWeightLookupKey = fontWeightLookupKey;
        private readonly Color? _customFontColour = customFontColour;
        private readonly bool _isDropShadowEnabled = isDropShadowEnabled;
        private readonly Color? _customDropShadowColour = customDropShadowColour;
        private readonly string _dateFormat = dateFormat;
        private readonly byte? _ordinalDaySuffixPosition = ordinalDaySuffixPosition;

        public Point AnchoredCornerScaledPosition => _anchoredCornerScaledPosition;

        public WindowVertexIdentifier AnchoredCorner => _anchoredCorner;

        public string FontFamilyName => _fontFamilyName;

        public int FontSize => _fontSize;

        public string FontWeightLookupKey => _fontWeightLookupKey;

        public Color? CustomFontColour => _customFontColour;

        public bool IsDropShadowEnabled => _isDropShadowEnabled;

        public Color? CustomDropShadowColour => _customDropShadowColour;

        public string DateFormat => _dateFormat;

        public byte? OrdinalDaySuffixPosition => _ordinalDaySuffixPosition;
    }
}
