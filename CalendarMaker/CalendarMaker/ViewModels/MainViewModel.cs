using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CalendarMaker.Models;

namespace CalendarMaker.ViewModels
{
    /// <summary>
    /// メイン画面の状態とロジックを管理する ViewModel。
    /// UIで扱うほぼ全てのデータがここを経由するため、拡張時はこのクラスを起点にすると迷いません。
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MonthPageViewModel> Pages { get; } = new();
        public ObservableCollection<MonthImageRow> ImageRows { get; } = new();

        // UI更新ループを避けるためのフラグ。画像リストをまとめて書き換える前にON。
        private bool _suspendImageRowSync;

        private int _selectedPageIndex;
        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set
            {
                if (_selectedPageIndex != value)
                {
                    _selectedPageIndex = value;
                    OnPropertyChanged(nameof(SelectedPageIndex));
                }
                UpdateCurrentPage();
            }
        }

        private MonthPageViewModel? _currentPage;
        public MonthPageViewModel? CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (!Equals(_currentPage, value))
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public CalendarSettings Settings { get; } = new();

        public IReadOnlyList<StartWeekday> StartWeekdayOptions { get; } = Enum.GetValues<StartWeekday>();

        public DateTime? StartMonthDateTime
        {
            get => new DateTime(Settings.StartMonth.Year, Settings.StartMonth.Month, 1);
            set
            {
                if (value is DateTime dt)
                {
                    Settings.StartMonth = new DateOnly(dt.Year, dt.Month, 1);
                    OnPropertyChanged(nameof(StartMonthDateTime));
                    UpdateImageRowDates();
                }
            }
        }

        public MainViewModel()
        {
            ImageRows.CollectionChanged += ImageRows_CollectionChanged;

            var previous = _suspendImageRowSync;
            _suspendImageRowSync = true;
            try
            {
                EnsureImageRows();
            }
            finally
            {
                _suspendImageRowSync = previous;
            }

            SyncImageRowsToSettings();
            BuildAllPages();
        }

        /// <summary>
        /// 設定値から月ページを再生成します。
        /// レイアウトやヘッダー文言をカスタマイズしたいときは、このメソッドを中心に手を入れてください。
        /// </summary>
        public void BuildAllPages()
        {
            SyncImageRowsToSettings();

            DateOnly? previouslySelected = CurrentPage is null
                ? null
                : new DateOnly(CurrentPage.Year, CurrentPage.Month, 1);

            var anniversaries = Settings.Anniversaries
                .GroupBy(a => a.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Label).Where(l => !string.IsNullOrWhiteSpace(l)).ToList());

            Pages.Clear();
            var first = Settings.StartMonth;

            for (int i = 0; i < 12; i++)
            {
                var month = first.AddMonths(i);
                var cells = CalendarMaker.Services.CalendarBuilder.BuildMonthGrid(
                    month.Year,
                    month.Month,
                    (DayOfWeek)Settings.StartWeekday,
                    anniversaries);

                var vm = new MonthPageViewModel
                {
                    Year = month.Year,
                    Month = month.Month,
                    MonthEnglish = month.ToDateTime(TimeOnly.MinValue).ToString("MMMM", CultureInfo.InvariantCulture),
                    HeaderEraText = CalendarMaker.Services.CalendarBuilder.FormatEraText(month),
                    Cells = new ObservableCollection<DayCell>(cells),
                    ImagePath = (i < Settings.MonthImagePaths.Count) ? Settings.MonthImagePaths[i] : string.Empty,
                    WeekdayLabels = CalendarMaker.Services.CalendarBuilder.BuildWeekdayLabels(Settings.StartWeekday)
                };

                Pages.Add(vm);
            }

            if (Pages.Count > 0)
            {
                int targetIndex = 0;
                if (previouslySelected.HasValue)
                {
                    for (int i = 0; i < Pages.Count; i++)
                    {
                        if (Pages[i].Year == previouslySelected.Value.Year && Pages[i].Month == previouslySelected.Value.Month)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }

                SelectedPageIndex = targetIndex;
                UpdateCurrentPage();
            }
            else
            {
                SelectedPageIndex = -1;
                CurrentPage = null;
            }

            RefreshImageRowsFromSettings();
        }

        public void Print()
        {
            if (Pages.Count == 0) return;
            CalendarMaker.Services.Printer.Print(Pages.ToList());
        }

        public void MoveImageUp(MonthImageRow row)
        {
            if (row is null) return;
            int index = ImageRows.IndexOf(row);
            if (index <= 0) return;

            var previous = _suspendImageRowSync;
            _suspendImageRowSync = true;
            try
            {
                ImageRows.Move(index, index - 1);
            }
            finally
            {
                _suspendImageRowSync = previous;
            }

            SyncImageRowsToSettings();
            BuildAllPages();
        }

        public void MoveImageDown(MonthImageRow row)
        {
            if (row is null) return;
            int index = ImageRows.IndexOf(row);
            if (index < 0 || index >= ImageRows.Count - 1) return;

            var previous = _suspendImageRowSync;
            _suspendImageRowSync = true;
            try
            {
                ImageRows.Move(index, index + 1);
            }
            finally
            {
                _suspendImageRowSync = previous;
            }

            SyncImageRowsToSettings();
            BuildAllPages();
        }

        public void AddAnniversary(DateTime date, string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return;

            Settings.Anniversaries.Add(new Anniversary
            {
                Date = DateOnly.FromDateTime(date),
                Label = label.Trim()
            });

            BuildAllPages();
        }

        /// <summary>
        /// 画像設定タブで使用する行データを12か月分そろえます。
        /// 13か月以上のレイアウトを目指す場合はループの上限を調整してください。
        /// </summary>
        private void EnsureImageRows()
        {
            var rowsSnapshot = ImageRows.ToList();
            foreach (var row in rowsSnapshot)
            {
                row.PropertyChanged -= MonthImageRow_PropertyChanged;
            }

            ImageRows.Clear();

            for (int i = 0; i < 12; i++)
            {
                var date = Settings.StartMonth.AddMonths(i);
                var path = (i < Settings.MonthImagePaths.Count) ? Settings.MonthImagePaths[i] : string.Empty;
                ImageRows.Add(new MonthImageRow { MonthDate = date, ImagePath = path });
            }
        }

        // 表示開始月を変えたときに各行の年月を更新。
        private void UpdateImageRowDates()
        {
            for (int i = 0; i < 12 && i < ImageRows.Count; i++)
            {
                ImageRows[i].MonthDate = Settings.StartMonth.AddMonths(i);
            }
        }

        private void UpdateCurrentPage()
        {
            if (SelectedPageIndex >= 0 && SelectedPageIndex < Pages.Count)
            {
                CurrentPage = Pages[SelectedPageIndex];
            }
            else
            {
                CurrentPage = null;
            }
        }

        // ImageRows -> Settings へ反映。保存系処理はこの結果を使います。
        private void SyncImageRowsToSettings()
        {
            for (int i = 0; i < Settings.MonthImagePaths.Count; i++)
            {
                string path = i < ImageRows.Count ? ImageRows[i].ImagePath : string.Empty;
                Settings.SetMonthImagePath(i, path);
            }
        }

        // Settings -> ImageRows へ反映。外部から設定を読み込んだ際の逆同期に使用。
        private void RefreshImageRowsFromSettings()
        {
            var previous = _suspendImageRowSync;
            _suspendImageRowSync = true;
            try
            {
                for (int i = 0; i < ImageRows.Count && i < Settings.MonthImagePaths.Count; i++)
                {
                    ImageRows[i].ImagePath = Settings.MonthImagePaths[i];
                }
            }
            finally
            {
                _suspendImageRowSync = previous;
            }
        }

        // 画像行の追加・削除にフックし、双方向の同期を維持。
        private void ImageRows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (MonthImageRow row in e.OldItems)
                {
                    row.PropertyChanged -= MonthImageRow_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (MonthImageRow row in e.NewItems)
                {
                    row.PropertyChanged += MonthImageRow_PropertyChanged;
                }
            }

            if (_suspendImageRowSync) return;

            SyncImageRowsToSettings();
        }

        // 個別行の画像パス変更を検知して再描画をトリガー。
        private void MonthImageRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_suspendImageRowSync) return;
            if (sender is not MonthImageRow) return;

            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(MonthImageRow.ImagePath))
            {
                SyncImageRowsToSettings();
                BuildAllPages();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



