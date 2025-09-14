using System;
using System.Globalization;
using System.Windows.Data;

namespace CalendarMaker.Converters
{
    // 元のdoubleから parameter 分を引く（parameter が負なら加算）
    public sealed class DoubleMinusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var baseVal = value is double d ? d : 12d;
            if (parameter != null && double.TryParse(parameter.ToString(), out var p))
                return Math.Max(1, baseVal - p);
            return Math.Max(1, baseVal - 4);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
