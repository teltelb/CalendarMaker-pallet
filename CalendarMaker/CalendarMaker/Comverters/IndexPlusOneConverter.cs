using System;
using System.Globalization;
using System.Windows.Data;

namespace CalendarMaker.Converters
{
    public sealed class IndexPlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? i + 1 : 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
