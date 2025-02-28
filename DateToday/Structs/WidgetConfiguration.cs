using Avalonia;

namespace DateToday.Structs
{
    struct WidgetConfiguration
    {
        // This is a simple data structure for user-configurable widget settings.

        public PixelPoint Position;
        public int FontSize;
        public string FontWeightLookupKey, DateFormat;
        public byte? OrdinalDaySuffixPosition;
    }
}
