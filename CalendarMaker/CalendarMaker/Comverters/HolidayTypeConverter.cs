using System;
using System.Globalization;
using System.Windows.Data;

namespace CalendarMaker.Converters
{
    public sealed class HolidayTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHoliday)
            {
                return isHoliday ? "祝日" : "記念日";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
