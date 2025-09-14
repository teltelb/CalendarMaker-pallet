using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CalendarMaker.Models;
using CalendarMaker.Services;

namespace CalendarMaker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MonthPageViewModel> Pages { get; } = new();

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
                    UpdateCurrentPage();
                }
            }
        }

        private MonthPageViewModel? _currentPage;
        public MonthPageViewModel? CurrentPage
        {
            get => _currentPage;
            private set { if (_currentPage != value) { _currentPage = value; OnPropertyChanged(nameof(CurrentPage)); } }
        }

        public CalendarSettings Settings { get; } = new();

        // DatePickerブリッジ
        public DateTime? StartMonthDateTime
        {
            get => new DateTime(Settings.StartMonth.Year, Settings.StartMonth.Month, 1);
            set
            {
                if (value is DateTime dt)
                {
                    Settings.StartMonth = new DateOnly(dt.Year, dt.Month, 1);
                    OnPropertyChanged(nameof(StartMonthDateTime));
                }
            }
        }

        public StartWeekday[] StartWeekdayOptions => (StartWeekday[])Enum.GetValues(typeof(StartWeekday));

        public MainViewModel() { BuildAllPages(); }

        public void BuildAllPages()
        {
            Pages.Clear();
            var first = Settings.StartMonth;

            for (int i = 0; i < 12; i++)
            {
                var m = first.AddMonths(i);
                var annivMap = Settings.Anniversaries.ToDictionary(a => a.Date, a => a.Label);
                var cells = CalendarBuilder.BuildMonthGrid(m.Year, m.Month,
                    (DayOfWeek)Settings.StartWeekday, annivMap);

                var en = m.ToDateTime(TimeOnly.MinValue).ToString("MMMM",
                    System.Globalization.CultureInfo.GetCultureInfo("en-US"));

                var vm = new MonthPageViewModel
                {
                    Year = m.Year,
                    Month = m.Month,
                    MonthEnglish = en,
                    HeaderEraText = CalendarBuilder.FormatEraText(m),
                    Cells = new ObservableCollection<DayCell>(cells),
                    ImagePath = Settings.MonthImagePaths[i],
                    Settings = Settings,
                    WeekdayLabels = CalendarBuilder.BuildWeekdayLabels(Settings.StartWeekday)
                };

                Pages.Add(vm);
            }

            if (Pages.Count > 0) { SelectedPageIndex = 0; CurrentPage = Pages[0]; }
            else { SelectedPageIndex = -1; CurrentPage = null; }
        }

        public void AddAnniversary(DateTime dt, string label)
        {
            Settings.Anniversaries.Add(new Anniversary { Date = new DateOnly(dt.Year, dt.Month, dt.Day), Label = label });
            BuildAllPages(); // 反映
        }

        public void ExportPdf() => PdfExporter.Export(Pages.ToList());
        public void Print() => Printer.Print(Pages.ToList());

        private void UpdateCurrentPage()
        {
            CurrentPage = (SelectedPageIndex >= 0 && SelectedPageIndex < Pages.Count)
                ? Pages[SelectedPageIndex] : null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
