using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CalendarMaker.Models
{
    // UIの開始曜日コンボボックスでそのまま使われる列挙体。
    // 和暦など独自並びにしたい場合はここへ新しい値を追加。
    public enum StartWeekday { Sunday = 0, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }

    /// <summary>
    /// カレンダーに表示する任意のメモ（ラベル）を表現します。
    /// 色やアイコンなどを追加したい場合はこのクラスを拡張してください。
    /// </summary>
    public class Anniversary
    {
        public DateOnly Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public bool IsManagedHoliday { get; set; }
        public override string ToString() => $"{Date:yyyy-MM-dd}: {Label}";
    }

    /// <summary>
    /// アプリ全体の状態を保持します。UIは基本的にこの設定と同期しています。
    /// 保存や読み込みを行う場合もこのクラスを経由すると把握しやすくなります。
    /// </summary>
    public class CalendarSettings
    {
        private static readonly Rect FullRect = new(0, 0, 1, 1);

        // カレンダーの起点となる年月。年度開始を固定したい場合はここを上書き。
        public DateOnly StartMonth { get; set; } = new(DateTime.Now.Year, DateTime.Now.Month, 1);

        // 曜日の表示順・グリッドの列（0列目）を決定します。
        public StartWeekday StartWeekday { get; set; } = StartWeekday.Sunday;

        // 日付セルに表示するイベント（記念日など）。
        public ObservableCollection<Anniversary> Anniversaries { get; } = new();

        // 祝日（外部データから取り込むもの）。一覧は長くなりがちなので別管理。
        public ObservableCollection<Anniversary> Holidays { get; } = new();

        // 月ごとの画像パスを保持。0〜11のインデックスで該当月を参照。
        public ObservableCollection<string> MonthImagePaths { get; } = new(Enumerable.Repeat(string.Empty, 12));

        // 月ごとの画像切り抜き（0〜1の相対座標）。PageView/プレビューで 3:2 にクリップする。
        public ObservableCollection<Rect> MonthImageCropRects { get; } = new(Enumerable.Repeat(FullRect, 12));

        public DateTimeOffset? HolidaysLastFetched { get; set; }
        public DateTimeOffset? HolidaysSourceLastModified { get; set; }
        public DateOnly? HolidaysRangeStart { get; set; }
        public DateOnly? HolidaysRangeEnd { get; set; }

        /// <summary>
        /// ViewModel側でリストと設定を同期するための補助メソッド。
        /// ページ数を増やす際は境界チェックも含めて調整してください。
        /// </summary>
        public void SetMonthImagePath(int index, string path)
        {
            if (index < 0 || index >= MonthImagePaths.Count) return;
            MonthImagePaths[index] = path ?? string.Empty;
        }

        public void SetMonthImageCropRect(int index, Rect rect)
        {
            if (index < 0 || index >= MonthImageCropRects.Count) return;
            MonthImageCropRects[index] = rect;
        }
    }
}

