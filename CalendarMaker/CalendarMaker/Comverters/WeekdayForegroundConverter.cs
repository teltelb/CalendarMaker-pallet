using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CalendarMaker.Models;

namespace CalendarMaker.Converters
{
    public sealed class WeekdayForegroundConverter : IMultiValueConverter
    {
        private static readonly Brush DefaultBrush = CreateBrush(ColorFromHex(0x22, 0x22, 0x22));
        private static readonly Brush SundayBrush = CreateBrush(ColorFromHex(0xCC, 0x00, 0x00));
        private static readonly Brush SaturdayBrush = CreateBrush(ColorFromHex(0x00, 0x66, 0xCC));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length < 2) return DefaultBrush;

            if (!TryGetInt(values[0], out int alternationIndex)) return DefaultBrush;

            int start = values[1] switch
            {
                StartWeekday sw => (int)sw,
                _ => TryGetInt(values[1], out int v) ? v : 0
            };

            int dayIndex = Mod(start + alternationIndex, 7);
            return dayIndex switch
            {
                0 => SundayBrush,
                6 => SaturdayBrush,
                _ => DefaultBrush
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static int Mod(int x, int m) => (x % m + m) % m;

        private static bool TryGetInt(object value, out int result)
        {
            switch (value)
            {
                case int i:
                    result = i;
                    return true;
                case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static Color ColorFromHex(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

        private static SolidColorBrush CreateBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}

