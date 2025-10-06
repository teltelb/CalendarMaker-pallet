using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CalendarMaker.Views;
using CalendarMaker.ViewModels;

namespace CalendarMaker
{
    public partial class MainWindow : Window
    {
        public MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => FitZoomToWindow();
            PreviewScroll.SizeChanged += (_, __) => FitZoomToWindow();
        }

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

        // Image row handlers
        private void ImageBrowseRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ViewModels.MonthImageRow row)
            {
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


        // Export: capture snapshot on UI, then run export on STA thread
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
