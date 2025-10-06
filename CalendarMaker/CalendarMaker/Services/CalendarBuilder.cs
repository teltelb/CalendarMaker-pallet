using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CalendarMaker.Models;

namespace CalendarMaker.Services
{
    public static class CalendarBuilder
    {
        private const int WeeksToDisplay = 6;
        private const int DaysPerWeek = 7;
        private const int MaxDisplayAnniversaries = 3;

        public static List<DayCell> BuildMonthGrid(int year, int month, DayOfWeek firstDayOfWeek, Dictionary<DateOnly, List<string>> annivs)
        {
            var first = new DateOnly(year, month, 1);
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int offset = ((int)first.DayOfWeek - (int)firstDayOfWeek + DaysPerWeek) % DaysPerWeek;
            int baseSlots = offset + daysInMonth;
            int targetCellCount = Math.Max(WeeksToDisplay * DaysPerWeek, (int)Math.Ceiling(baseSlots / (double)DaysPerWeek) * DaysPerWeek);
            var cells = new List<DayCell>(targetCellCount);

            var prevMonth = first.AddMonths(-1);
            int prevDays = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            for (int i = 0; i < offset; i++)
            {
                var d = new DateOnly(prevMonth.Year, prevMonth.Month, prevDays - offset + 1 + i);
                cells.Add(MakeCell(d, false, annivs));
            }

            for (int d = 1; d <= daysInMonth; d++)
                cells.Add(MakeCell(new DateOnly(year, month, d), true, annivs));

            var nextStart = first.AddMonths(1);
            while (cells.Count < targetCellCount)
            {
                var dayIndex = cells.Count - (offset + daysInMonth) + 1;
                var d = new DateOnly(nextStart.Year, nextStart.Month, dayIndex);
                cells.Add(MakeCell(d, false, annivs));
            }

            int rowCount = targetCellCount / DaysPerWeek;
            for (int i = 0; i < cells.Count; i++)
            {
                int row = i / DaysPerWeek;
                int column = i % DaysPerWeek;
                var cell = cells[i];
                cell.RowIndex = row;
                cell.ColumnIndex = column;
                cell.IsLastRow = row == rowCount - 1;
                cell.IsLastColumn = column == DaysPerWeek - 1;
            }

            return cells;
        }

        static DayCell MakeCell(DateOnly date, bool current, Dictionary<DateOnly, List<string>> annivs)
        {
            if (!annivs.TryGetValue(date, out var lines))
            {
                lines = new List<string>();
            }

            var filtered = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
            var display = filtered.Take(MaxDisplayAnniversaries).ToList();
            while (display.Count < MaxDisplayAnniversaries) display.Add(string.Empty);

            return new DayCell
            {
                Date = date,
                IsCurrentMonth = current,
                AnniversaryLines = filtered,
                AnniversaryDisplayLines = display,
                AnniversaryLabel = filtered.Count > 0 ? string.Join(Environment.NewLine, filtered) : null
            };
        }

        public static string FormatEraText(DateOnly d)
        {
            var culture = new CultureInfo("ja-JP") { DateTimeFormat = { Calendar = new JapaneseCalendar() } };
            var dt = new DateTime(d.Year, d.Month, d.Day);
            var wareki = dt.ToString("ggy年", culture);
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
