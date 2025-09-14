using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CalendarMaker.Models
{
    public enum StartWeekday { Sunday = 0, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }

    public class Anniversary
    {
        public DateOnly Date { get; set; }
        public string Label { get; set; } = "";
        public override string ToString() => $"{Date:yyyy-MM-dd}: {Label}";
    }

    public class CalendarSettings
    {
        public DateOnly StartMonth { get; set; } = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);
        public StartWeekday StartWeekday { get; set; } = StartWeekday.Sunday;

        public ObservableCollection<Anniversary> Anniversaries { get; } = new();

        public ObservableCollection<string> MonthImagePaths { get; }
            = new ObservableCollection<string>(Enumerable.Repeat(string.Empty, 12));

        public void SetMonthImagePath(int index, string path)
        {
            if (index < 0 || index >= MonthImagePaths.Count) return;
            MonthImagePaths[index] = path ?? "";
        }

        // 既定フォント（Impact指定）
        public FontFamily HeaderFont { get; set; } = new FontFamily("Impact");
        public double HeaderFontSize { get; set; } = 128;

        public FontFamily BodyFont { get; set; } = new FontFamily("Impact");
        public double BodyFontSize { get; set; } = 26;
    }
}
