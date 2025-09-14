using System;
using System.Collections.Generic;
using System.Globalization;
using CalendarMaker.Models;

namespace CalendarMaker.Services
{
    public static class CalendarBuilder
    {
        public static List<DayCell> BuildMonthGrid(int year, int month, DayOfWeek firstDayOfWeek,
            Dictionary<DateOnly, string> annivs)
        {
            var first = new DateOnly(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);

            // 開始曜日のオフセット
            int offset = ((int)first.DayOfWeek - (int)firstDayOfWeek + 7) % 7;

            var cells = new List<DayCell>(42);

            // 前月の埋め
            var prevMonth = first.AddMonths(-1);
            int prevDays = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            for (int i = 0; i < offset; i++)
            {
                var d = new DateOnly(prevMonth.Year, prevMonth.Month, prevDays - offset + 1 + i);
                cells.Add(MakeCell(d, false, annivs));
            }

            // 今月
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                cells.Add(MakeCell(date, true, annivs));
            }

            // 翌月の埋め
            var nextStart = first.AddMonths(1);
            while (cells.Count < 42)
            {
                var d = new DateOnly(nextStart.Year, nextStart.Month, (cells.Count - (offset + daysInMonth) + 1));
                cells.Add(MakeCell(d, false, annivs));
            }

            return cells;
        }

        static DayCell MakeCell(DateOnly date, bool current, Dictionary<DateOnly, string> annivs)
            => new()
            {
                Date = date,
                IsCurrentMonth = current,
                AnniversaryLabel = annivs.TryGetValue(date, out var s) ? s : null
            };

        public static string FormatEraText(DateOnly d)
        {
            var culture = new CultureInfo("ja-JP");
            culture.DateTimeFormat.Calendar = new JapaneseCalendar();
            var dt = new DateTime(d.Year, d.Month, d.Day, culture.DateTimeFormat.Calendar);
            string wareki = dt.ToString("ggy年", culture); // 令和7年 等
            return $"{d.Year}年（{wareki}）";
        }

        public static string[] BuildWeekdayLabels(StartWeekday start)
        {
            string[] baseJP = { "日", "月", "火", "水", "木", "金", "土" };
            var result = new string[7];
            int s = (int)start;
            for (int i = 0; i < 7; i++) result[i] = baseJP[(s + i) % 7];
            return result;
        }
    }
}
