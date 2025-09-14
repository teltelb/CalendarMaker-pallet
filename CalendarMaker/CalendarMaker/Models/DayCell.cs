using System;

namespace CalendarMaker.Models
{
    public class DayCell
    {
        public DateOnly Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public string? AnniversaryLabel { get; set; }
    }
}
