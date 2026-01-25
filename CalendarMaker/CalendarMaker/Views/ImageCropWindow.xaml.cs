using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CalendarMaker.ViewModels;

namespace CalendarMaker.Views
{
    public partial class ImageCropWindow : Window
    {
        private readonly double _targetAspectRatio;
        private static readonly Rect FullRect = new(0, 0, 1, 1);

        private readonly MonthImageRow _row;
        private readonly double _imageAspectRatio;
        private readonly double _normalizedRatio;

        private Rect _imageDisplayRect = Rect.Empty;
        private Rect _cropRect = FullRect;

        private bool _dragging;
        private Point _dragStart;
        private Rect _dragStartCrop;

        public ImageCropWindow(MonthImageRow row)
            : this(row, 3.0 / 2.0)
        {
        }

        public ImageCropWindow(MonthImageRow row, double targetAspectRatio)
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            _targetAspectRatio = targetAspectRatio <= 0 ? 3.0 / 2.0 : targetAspectRatio;
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(_row.ImagePath) || !File.Exists(_row.ImagePath))
            {
                MessageBox.Show("画像ファイルが見つかりません。");
                Close();
                return;
            }

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(_row.ImagePath, UriKind.Absolute);
            bi.EndInit();
            bi.Freeze();

            PreviewImage.Source = bi;

            _imageAspectRatio = bi.PixelHeight > 0 ? (double)bi.PixelWidth / bi.PixelHeight : _targetAspectRatio;
            _normalizedRatio = _targetAspectRatio / _imageAspectRatio;

            _cropRect = NormalizeToTarget(_row.CropRect);
            Loaded += (_, __) => UpdateOverlay();
        }

        private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateOverlay();

        private void UpdateOverlay()
        {
            _imageDisplayRect = ComputeDisplayedImageRect();
            if (_imageDisplayRect.IsEmpty)
                return;

            _cropRect = ClampToBounds(_cropRect);

            var r = new Rect(
                _imageDisplayRect.X + _cropRect.X * _imageDisplayRect.Width,
                _imageDisplayRect.Y + _cropRect.Y * _imageDisplayRect.Height,
                _cropRect.Width * _imageDisplayRect.Width,
                _cropRect.Height * _imageDisplayRect.Height);

            Canvas.SetLeft(CropOverlay, r.X);
            Canvas.SetTop(CropOverlay, r.Y);
            CropOverlay.Width = r.Width;
            CropOverlay.Height = r.Height;
        }

        private Rect ComputeDisplayedImageRect()
        {
            double hostW = PreviewHost.ActualWidth;
            double hostH = PreviewHost.ActualHeight;
            if (hostW <= 0 || hostH <= 0) return Rect.Empty;

            double hostRatio = hostW / hostH;
            if (hostRatio >= _imageAspectRatio)
            {
                double h = hostH;
                double w = h * _imageAspectRatio;
                double x = (hostW - w) / 2;
                return new Rect(x, 0, w, h);
            }

            double w2 = hostW;
            double h2 = w2 / _imageAspectRatio;
            double y2 = (hostH - h2) / 2;
            return new Rect(0, y2, w2, h2);
        }

        private void CropOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_imageDisplayRect.IsEmpty) return;
            _dragging = true;
            _dragStart = e.GetPosition(PreviewHost);
            _dragStartCrop = _cropRect;
            CropOverlay.CaptureMouse();
            e.Handled = true;
        }

        private void CropOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || _imageDisplayRect.IsEmpty) return;

            var pos = e.GetPosition(PreviewHost);
            double dx = pos.X - _dragStart.X;
            double dy = pos.Y - _dragStart.Y;

            double nx = _dragStartCrop.X + dx / _imageDisplayRect.Width;
            double ny = _dragStartCrop.Y + dy / _imageDisplayRect.Height;

            _cropRect = ClampToBounds(new Rect(nx, ny, _dragStartCrop.Width, _dragStartCrop.Height));
            UpdateOverlay();
            e.Handled = true;
        }

        private void CropOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            if (CropOverlay.IsMouseCaptured) CropOverlay.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void PreviewHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_imageDisplayRect.IsEmpty) return;
            if (ReferenceEquals(e.OriginalSource, CropOverlay)) return;

            var pos = e.GetPosition(PreviewHost);
            if (!_imageDisplayRect.Contains(pos)) return;

            double cx = (pos.X - _imageDisplayRect.X) / _imageDisplayRect.Width;
            double cy = (pos.Y - _imageDisplayRect.Y) / _imageDisplayRect.Height;

            _cropRect = CenterAt(_cropRect, cx, cy);
            UpdateOverlay();
        }

        private void PreviewHost_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_imageDisplayRect.IsEmpty) return;

            double factor = e.Delta > 0 ? 0.9 : 1.1;

            double cx = _cropRect.X + _cropRect.Width / 2;
            double cy = _cropRect.Y + _cropRect.Height / 2;

            double newH = _cropRect.Height * factor;
            double minH = 0.05;

            if (_normalizedRatio <= 1)
            {
                double maxH = 1;
                if (newH < minH) newH = minH;
                if (newH > maxH) newH = maxH;
            }
            else
            {
                double maxH = 1 / _normalizedRatio;
                if (newH < minH) newH = minH;
                if (newH > maxH) newH = maxH;
            }

            double newW = newH * _normalizedRatio;
            var rect = new Rect(cx - newW / 2, cy - newH / 2, newW, newH);
            _cropRect = ClampToBounds(rect);
            UpdateOverlay();
            e.Handled = true;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _cropRect = DefaultCrop();
            UpdateOverlay();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _row.CropRect = _cropRect;
            DialogResult = true;
            Close();
        }

        private Rect DefaultCrop()
        {
            if (_imageAspectRatio >= _targetAspectRatio)
            {
                double width = _targetAspectRatio / _imageAspectRatio;
                return new Rect((1 - width) / 2, 0, width, 1);
            }

            double height = _imageAspectRatio / _targetAspectRatio;
            return new Rect(0, (1 - height) / 2, 1, height);
        }

        private Rect NormalizeToTarget(Rect rect)
        {
            var centerX = rect.X + rect.Width / 2;
            var centerY = rect.Y + rect.Height / 2;

            if (double.IsNaN(centerX) || double.IsNaN(centerY)) return DefaultCrop();

            double w = rect.Width;
            double h = rect.Height;

            if (w <= 0 || h <= 0) return DefaultCrop();

            double desiredW = h * _normalizedRatio;
            double desiredH = w / _normalizedRatio;

            if (_normalizedRatio <= 1)
            {
                w = desiredW;
            }
            else
            {
                h = desiredH;
            }

            var normalized = new Rect(centerX - w / 2, centerY - h / 2, w, h);
            return ClampToBounds(normalized);
        }

        private Rect CenterAt(Rect rect, double centerX, double centerY)
        {
            var moved = new Rect(centerX - rect.Width / 2, centerY - rect.Height / 2, rect.Width, rect.Height);
            return ClampToBounds(moved);
        }

        private static Rect ClampToBounds(Rect rect)
        {
            double x = rect.X;
            double y = rect.Y;
            double w = rect.Width;
            double h = rect.Height;

            if (w <= 0) w = 0.01;
            if (h <= 0) h = 0.01;
            if (w > 1) w = 1;
            if (h > 1) h = 1;

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x + w > 1) x = 1 - w;
            if (y + h > 1) y = 1 - h;

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            return new Rect(x, y, w, h);
        }
    }
}

