using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarMaker.Models;
using CalendarMaker.Services;

namespace CalendarMaker.ViewModels
{
    /// <summary>
    /// ���C����ʂ̏�Ԃƃ��W�b�N��Ǘ����� ViewModel�B
    /// UI�ň����قڑS�Ẵf�[�^��������o�R���邽�߁A�g�����͂��̃N���X��N�_�ɂ���Ɩ����܂���B
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<MonthPageViewModel> Pages { get; } = new();
        public ObservableCollection<MonthImageRow> ImageRows { get; } = new();

        private readonly NationalHolidayService _holidayService = new();

        // UI�X�V���[�v�����邽�߂̃t���O�B�摜���X�g��܂Ƃ߂ď���������O��ON�B
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
        /// �ݒ�l���猎�y�[�W��Đ������܂��B
        /// ���C�A�E�g��w�b�_�[������J�X�^�}�C�Y�������Ƃ��́A���̃��\�b�h�𒆐S�Ɏ�����Ă��������B
        /// </summary>
        public void BuildAllPages()
        {
            SyncImageRowsToSettings();

            DateOnly? previouslySelected = CurrentPage is null
                ? null
                : new DateOnly(CurrentPage.Year, CurrentPage.Month, 1);

            var combinedEvents = Settings.Anniversaries.Concat(Settings.Holidays);

            var anniversaries = combinedEvents
                .GroupBy(a => a.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Label).Where(l => !string.IsNullOrWhiteSpace(l)).ToList());

            var holidayDates = Settings.Holidays.Select(h => h.Date).ToHashSet();

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

                foreach (var cell in cells)
                {
                    cell.IsHoliday = holidayDates.Contains(cell.Date);
                }

                var vm = new MonthPageViewModel
                {
                    Year = month.Year,
                    Month = month.Month,
                    MonthEnglish = month.ToDateTime(TimeOnly.MinValue).ToString("MMMM", CultureInfo.InvariantCulture),
                    HeaderEraText = CalendarMaker.Services.CalendarBuilder.FormatEraText(month),
                    Cells = new ObservableCollection<DayCell>(cells),
                    ImagePath = (i < Settings.MonthImagePaths.Count) ? Settings.MonthImagePaths[i] : string.Empty,
                    WeekdayLabels = CalendarMaker.Services.CalendarBuilder.BuildWeekdayLabels(Settings.StartWeekday),
                    StartWeekday = Settings.StartWeekday
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

        public void SetMonthImagesFromIndex(int startIndex, IEnumerable<string> imagePaths)
        {
            if (imagePaths is null) return;
            if (ImageRows.Count == 0) return;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= ImageRows.Count) return;

            var paths = imagePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Where(File.Exists)
                .Where(IsImageFilePath)
                .ToList();

            if (paths.Count == 0) return;

            var previous = _suspendImageRowSync;
            _suspendImageRowSync = true;
            try
            {
                int index = startIndex;
                foreach (var path in paths)
                {
                    if (index >= ImageRows.Count) break;
                    ImageRows[index].ImagePath = path;
                    index++;
                }
            }
            finally
            {
                _suspendImageRowSync = previous;
            }

            SyncImageRowsToSettings();
            BuildAllPages();
        }

        private static bool IsImageFilePath(string path)
        {
            var ext = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext)) return false;

            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".tif", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> RefreshHolidaysAsync(bool suppressErrors = true, CancellationToken cancellationToken = default)
        {
            var startMonth = Settings.StartMonth;
            var endMonth = startMonth.AddMonths(11);

            try
            {
                var result = await _holidayService.FetchHolidaysAsync(
                    startMonth.Year,
                    endMonth.Year,
                    Settings.HolidaysSourceLastModified,
                    cancellationToken);

                Settings.HolidaysLastFetched = DateTimeOffset.Now;

                if (result.NotModified)
                {
                    if (Settings.HolidaysRangeStart == startMonth && Settings.HolidaysRangeEnd == endMonth)
                    {
                        BuildAllPages();
                        return true;
                    }

                    result = await _holidayService.FetchHolidaysAsync(
                        startMonth.Year,
                        endMonth.Year,
                        null,
                        cancellationToken);
                }

                Settings.Holidays.Clear();

                foreach (var holiday in result.Holidays)
                {
                    if (holiday.Date < startMonth || holiday.Date > endMonth) continue;

                    if (!Settings.Anniversaries.Any(a => a.Date == holiday.Date && string.Equals(a.Label, holiday.Name, StringComparison.Ordinal)))
                    {
                        var label = holiday.Name;
                        if (label.Contains("振替休日", StringComparison.Ordinal))
                        {
                            label = "振替休日";
                        }

                        Settings.Holidays.Add(new Anniversary
                        {
                            Date = holiday.Date,
                            Label = label,
                            IsManagedHoliday = true
                        });
                    }
                }

                Settings.HolidaysSourceLastModified = result.LastModified;
                Settings.HolidaysRangeStart = startMonth;
                Settings.HolidaysRangeEnd = endMonth;
                BuildAllPages();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch when (suppressErrors)
            {
                return false;
                // �l�b�g���[�N�G���[�Ȃǂَ͖E���A�����f�[�^�ő��s
            }
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
        /// �摜�ݒ�^�u�Ŏg�p����s�f�[�^��12���������낦�܂��B
        /// 13�����ȏ�̃��C�A�E�g��ڎw���ꍇ�̓��[�v�̏���𒲐����Ă��������B
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

        // �\���J�n����ς����Ƃ��Ɋe�s�̔N����X�V�B
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

        // ImageRows -> Settings �֔��f�B�ۑ��n�����͂��̌��ʂ�g���܂��B
        private void SyncImageRowsToSettings()
        {
            for (int i = 0; i < Settings.MonthImagePaths.Count; i++)
            {
                string path = i < ImageRows.Count ? ImageRows[i].ImagePath : string.Empty;
                Settings.SetMonthImagePath(i, path);
            }
        }

        // Settings -> ImageRows �֔��f�B�O������ݒ��ǂݍ��񂾍ۂ̋t�����Ɏg�p�B
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

        // �摜�s�̒ǉ��E�폜�Ƀt�b�N���A�o�����̓�����ێ��B
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

        // �ʍs�̉摜�p�X�ύX����m���čĕ`���g���K�[�B
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



