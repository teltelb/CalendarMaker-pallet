using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalendarMaker.Models
{
    public enum StartWeekday { Sunday = 0, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }

    public class Anniversary
    {
        public DateOnly Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public override string ToString() => $"{Date:yyyy-MM-dd}: {Label}";
    }

    public class CalendarSettings
    {
        public DateOnly StartMonth { get; set; } = new(DateTime.Now.Year, DateTime.Now.Month, 1);
        public StartWeekday StartWeekday { get; set; } = StartWeekday.Sunday;
        public ObservableCollection<Anniversary> Anniversaries { get; } = new();
        public ObservableCollection<string> MonthImagePaths { get; } = new(Enumerable.Repeat(string.Empty, 12));

        public void SetMonthImagePath(int index, string path)
        {
            if (index < 0 || index >= MonthImagePaths.Count) return;
            MonthImagePaths[index] = path ?? string.Empty;
        }
    }
}
