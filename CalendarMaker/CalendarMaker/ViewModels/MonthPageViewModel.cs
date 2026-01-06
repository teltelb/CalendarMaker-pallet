using System.Collections.ObjectModel;
using CalendarMaker.Models;

namespace CalendarMaker.ViewModels
{
    /// <summary>
    /// 1ページ分の表示データコンテナー。
    /// レイアウト側で表示したい情報はここへプロパティを追加すると扱いやすい。
    /// </summary>
    public class MonthPageViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthEnglish { get; set; } = string.Empty;
        public string HeaderEraText { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public ObservableCollection<DayCell> Cells { get; set; } = new();
        public string[] WeekdayLabels { get; set; } = new string[7];
        public StartWeekday StartWeekday { get; set; }
        public double? ImageAspectRatio { get; set; }
    }
}
