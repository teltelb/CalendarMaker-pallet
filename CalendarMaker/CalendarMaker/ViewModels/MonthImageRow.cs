using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CalendarMaker.ViewModels
{
    /// <summary>
    /// 月ごとの画像設定をUIと同期するためのビューモデル。
    /// プレビュー表示や追加のメタ情報を扱いたい場合はこのクラスを拡張します。
    /// </summary>
    public class MonthImageRow : INotifyPropertyChanged
    {
        private DateOnly _monthDate;
        public DateOnly MonthDate { get => _monthDate; set { _monthDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonthLabel)); } }

        private string _imagePath = string.Empty;
        public string ImagePath { get => _imagePath; set { if (_imagePath == value) return; _imagePath = value ?? string.Empty; TryLoadPreview(_imagePath); OnPropertyChanged(); OnPropertyChanged(nameof(ImageName)); } }

        public string MonthLabel => $"{MonthDate.Year}/{MonthDate.Month:00}";
        public string ImageName => string.IsNullOrEmpty(_imagePath) ? "" : Path.GetFileName(_imagePath);

        private ImageSource? _preview;
        public ImageSource? Preview { get => _preview; private set { _preview = value; OnPropertyChanged(); } }

        private double? _aspectRatio;
        public double? AspectRatio { get => _aspectRatio; private set { if (_aspectRatio == value) return; _aspectRatio = value; OnPropertyChanged(); } }

        // 画像パスから軽量プレビューを読み込み。
        // 読み込みサイズやサムネイル品質を変えたい場合はここを調整。
        private void TryLoadPreview(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { Preview = null; AspectRatio = null; return; }
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = 160;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                AspectRatio = bi.PixelHeight > 0 ? (double)bi.PixelWidth / bi.PixelHeight : (double?)null;
                bi.Freeze();
                Preview = bi;
            }
            catch { Preview = null; AspectRatio = null; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
