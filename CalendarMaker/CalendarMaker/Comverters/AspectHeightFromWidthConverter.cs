using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CalendarMaker.Converters
{
    public sealed class AspectHeightFromWidthConverter : IMultiValueConverter
    {
        public double PageAspectFactor { get; set; } = 2.0 / 3.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is null || values.Length == 0) return 0d;

            double width = values[0] is double w ? w : 0d;
            if (width <= 0d) return 0d;

            return width * PageAspectFactor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}