using System;
using System.Collections.Generic;

namespace CalendarMaker.Models
{
    /// <summary>
    /// 日付セル1マス分の情報。
    /// 表示内容を増やしたい場合はプロパティを追加して XAML へバインド。
    /// </summary>
    public class DayCell
    {
        public DateOnly Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public string? AnniversaryLabel { get; set; }
        public IReadOnlyList<string> AnniversaryLines { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> AnniversaryDisplayLines { get; set; } = Array.Empty<string>();
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }
        public bool IsLastRow { get; set; }
        public bool IsLastColumn { get; set; }
    }
}
