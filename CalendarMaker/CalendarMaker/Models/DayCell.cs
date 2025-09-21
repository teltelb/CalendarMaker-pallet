using System;
using System.Collections.Generic;

namespace CalendarMaker.Models
{
    public class DayCell
    {
        public DateOnly Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public string? AnniversaryLabel { get; set; }
        public IReadOnlyList<string> AnniversaryLines { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> AnniversaryDisplayLines { get; set; } = Array.Empty<string>();
    }
}
