using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CalendarMaker.Views;
using CalendarMaker.ViewModels;

namespace CalendarMaker
{
    /// <summary>
    /// アプリのメインウィンドウ。UIのイベントバインディングやビュー操作の調整はここで行います。
    /// </summary>
    public partial class MainWindow : Window
    {
        // マウスホイールで拡大縮小するときのステップ幅。好きな感度に変更可。
        private const double ZoomStep = 0.1;
        private bool _isPanning;
        private Point _panStart;
        private Point _panOrigin;

        public MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            PreviewScroll.Focusable = false;
            PreviewScroll.PreviewMouseWheel += PreviewScroll_PreviewMouseWheel;
            PreviewScroll.PreviewMouseLeftButtonDown += PreviewScroll_PreviewMouseLeftButtonDown;
            PreviewScroll.PreviewMouseMove += PreviewScroll_PreviewMouseMove;
            PreviewScroll.PreviewMouseLeftButtonUp += PreviewScroll_PreviewMouseLeftButtonUp;
            PreviewScroll.MouseLeave += PreviewScroll_MouseLeave;
            PreviewScroll.LostMouseCapture += PreviewScroll_LostMouseCapture;

            Loaded += MainWindow_Loaded;
            PreviewScroll.SizeChanged += (_, __) => FitZoomToWindow();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FitZoomToWindow();
            try
            {
                await VM.RefreshHolidaysAsync();
            }
            catch (OperationCanceledException)
            {
                // ignore cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Holiday fetch failed: {ex}");
            }
        }

        // プレビュー領域のサイズに合わせてズーム倍率を自動調整。
        private void FitZoomToWindow()
        {
            if (PreviewScroll.ViewportWidth <= 0 || PreviewScroll.ViewportHeight <= 0) return;
            double pad = 32;
            double sx = (PreviewScroll.ViewportWidth - pad) / PageSpec.PageDipW;
            double sy = (PreviewScroll.ViewportHeight - pad) / PageSpec.PageDipH;
            ZoomSlider.Value = Math.Max(0.1, Math.Min(sx, sy));
        }

        private void Rebuild_Click(object sender, RoutedEventArgs e) => VM.BuildAllPages();

        private void Print_Click(object sender, RoutedEventArgs e) => VM.Print();

        // ズームしたときにカーソル位置を基準にスクロール位置を補正。
        private void PreviewScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double oldZoom = ZoomSlider.Value;
            double step = e.Delta > 0 ? ZoomStep : -ZoomStep;
            double newZoom = Math.Max(ZoomSlider.Minimum, Math.Min(ZoomSlider.Maximum, oldZoom + step));
            if (Math.Abs(newZoom - oldZoom) < 0.0001) return;

            Point cursorInScroll = e.GetPosition(PreviewScroll);
            Point cursorOnContent = e.GetPosition(ZoomHost);

            ZoomSlider.Value = newZoom;
            PreviewScroll.UpdateLayout();

            double scaleRatio = newZoom / oldZoom;
            double targetContentX = cursorOnContent.X * scaleRatio;
            double targetContentY = cursorOnContent.Y * scaleRatio;

            ScrollTo(targetContentX - cursorInScroll.X, targetContentY - cursorInScroll.Y);
            e.Handled = true;
        }

        // 左ドラッグでパン操作を開始。
        private void PreviewScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PreviewScroll.Focus();
            _isPanning = true;
            _panStart = e.GetPosition(PreviewScroll);
            _panOrigin = new Point(PreviewScroll.HorizontalOffset, PreviewScroll.VerticalOffset);
            PreviewScroll.CaptureMouse();
            PreviewScroll.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void PreviewScroll_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning || e.LeftButton != MouseButtonState.Pressed) return;

            Point current = e.GetPosition(PreviewScroll);
            Vector delta = current - _panStart;

            double offsetX = _panOrigin.X - delta.X;
            double offsetY = _panOrigin.Y - delta.Y;

            ScrollTo(offsetX, offsetY);
            e.Handled = true;
        }

        private void PreviewScroll_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndPan();

        private void PreviewScroll_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndPan();
            }
        }

        private void PreviewScroll_LostMouseCapture(object sender, MouseEventArgs e) => EndPan();

        // パン操作の終了処理。カーソルやキャプチャ状態をリセット。
        private void EndPan()
        {
            if (!_isPanning) return;

            _isPanning = false;
            if (PreviewScroll.IsMouseCaptured) PreviewScroll.ReleaseMouseCapture();
            PreviewScroll.Cursor = Cursors.Arrow;
        }

        private void ScrollTo(double offsetX, double offsetY)
        {
            PreviewScroll.ScrollToHorizontalOffset(Clamp(offsetX, 0, PreviewScroll.ScrollableWidth));
            PreviewScroll.ScrollToVerticalOffset(Clamp(offsetY, 0, PreviewScroll.ScrollableHeight));
        }

        private static double Clamp(double value, double min, double max)
            => value < min ? min : (value > max ? max : value);

        // 画像設定タブ関連のイベントハンドラ群
        private void ImageBrowseRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ViewModels.MonthImageRow row)
            {
                // 対応する拡張子を増やしたい場合はフィルター文字列を編集。
                var dlg = new OpenFileDialog { Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|すべて|*.*" };
                if (dlg.ShowDialog() == true) row.ImagePath = dlg.FileName;
            }
        }
        private void ImageUpRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ViewModels.MonthImageRow row) VM.MoveImageUp(row);
        }
        private void ImageDownRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ViewModels.MonthImageRow row) VM.MoveImageDown(row);
        }

        private void AddAnniv_Click(object sender, RoutedEventArgs e)
        {
            if (AnnivDate.SelectedDate is DateTime dt && !string.IsNullOrWhiteSpace(AnnivLabel.Text))
            {
                VM.AddAnniversary(dt, AnnivLabel.Text.Trim());
                AnnivLabel.Text = "";
            }
        }


        // エクスポート：UIをビットマップ化して別スレッドでPDF書き出し。
        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var pagesSnapshot = VM.Pages.ToList();

            var win = new ProgressWindow { Owner = this };
            win.Show();

            try
            {
                await RunOnStaThreadAsync(() =>
                    Services.PdfExporter.Export(pagesSnapshot, win.Reporter, win.Token));

                if (!win.Token.IsCancellationRequested) MessageBox.Show("PDFを書き出しました。");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("出力をキャンセルしました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF出力でエラーが発生しました。\n" + ex.Message);
            }
            finally { win.Close(); }
        }

        private static Task RunOnStaThreadAsync(Action action)
        {
            var tcs = new TaskCompletionSource<object?>();
            var th = new Thread(() =>
            {
                try { action(); tcs.SetResult(null); }
                catch (OperationCanceledException) { tcs.SetCanceled(); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            th.IsBackground = true;
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
            return tcs.Task;
        }
    }
}



