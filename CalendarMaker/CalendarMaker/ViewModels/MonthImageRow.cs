using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
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
        private static readonly Rect FullRect = new(0, 0, 1, 1);
        private const double TargetAspectRatio = 3.0 / 2.0;

        private DateOnly _monthDate;
        public DateOnly MonthDate { get => _monthDate; set { _monthDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonthLabel)); } }

        private string _imagePath = string.Empty;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value) return;
                _imagePath = value ?? string.Empty;
                TryLoadPreview(_imagePath);
                ResetCropToDefault();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImageName));
            }
        }

        public string MonthLabel => $"{MonthDate.Year}/{MonthDate.Month:00}";
        public string ImageName => string.IsNullOrEmpty(_imagePath) ? "" : Path.GetFileName(_imagePath);

        private ImageSource? _preview;
        public ImageSource? Preview { get => _preview; private set { _preview = value; OnPropertyChanged(); } }

        private double? _aspectRatio;
        public double? AspectRatio { get => _aspectRatio; private set { if (_aspectRatio == value) return; _aspectRatio = value; OnPropertyChanged(); } }

        // 画像パスから軽量プレビューを読み込み。
        // 読み込みサイズやサムネイル品質を変えたい場合はここを調整。
        private Rect _cropRect = FullRect;
        public Rect CropRect
        {
            get => _cropRect;
            set
            {
                if (_cropRect == value) return;
                _cropRect = NormalizeCropRect(value);
                OnPropertyChanged();
            }
        }

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

        public void ResetCropToDefault()
        {
            if (AspectRatio is not double ratio || ratio <= 0)
            {
                CropRect = FullRect;
                return;
            }

            if (ratio >= TargetAspectRatio)
            {
                double width = TargetAspectRatio / ratio;
                CropRect = new Rect((1 - width) / 2, 0, width, 1);
                return;
            }

            double height = ratio / TargetAspectRatio;
            CropRect = new Rect(0, (1 - height) / 2, 1, height);
        }

        private static Rect NormalizeCropRect(Rect rect)
        {
            double x = double.IsNaN(rect.X) ? 0 : rect.X;
            double y = double.IsNaN(rect.Y) ? 0 : rect.Y;
            double w = double.IsNaN(rect.Width) ? 1 : rect.Width;
            double h = double.IsNaN(rect.Height) ? 1 : rect.Height;

            if (w <= 0) w = 0.01;
            if (h <= 0) h = 0.01;

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + w > 1) x = 1 - w;
            if (y + h > 1) y = 1 - h;

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            return new Rect(x, y, w, h);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
