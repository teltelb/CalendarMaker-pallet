using System;
using System.Globalization;
using System.Windows.Data;

namespace CalendarMaker.Converters
{
    public sealed class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double baseValue = value is double d ? d : 0d;
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double factor))
            {
                return baseValue * factor;
            }
            return baseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
