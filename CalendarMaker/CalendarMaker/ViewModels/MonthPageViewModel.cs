using System.Collections.ObjectModel;
using CalendarMaker.Models;

namespace CalendarMaker.ViewModels
{
    public class MonthPageViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthEnglish { get; set; } = "";
        public string HeaderEraText { get; set; } = "";
        public string? ImagePath { get; set; }
        public ObservableCollection<DayCell> Cells { get; set; } = new();
        public CalendarSettings Settings { get; set; } = default!;
        public string[] WeekdayLabels { get; set; } = new string[7];
    }
}
