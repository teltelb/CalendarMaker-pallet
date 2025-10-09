using CalendarMaker.ViewModels;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CalendarMaker.Services
{
    /// <summary>
    /// PDF出力の手順をまとめたヘルパー。
    /// ファイル名や保存形式を増やしたい場合はこのクラスで制御します。
    /// </summary>
    public static class PdfExporter
    {
        const int Dpi = PageSpec.PdfDpi;
        const int PagePxW = PageSpec.PagePxW;
        const int PagePxH = PageSpec.PagePxH;

        public static void Export(IList<MonthPageViewModel> pages, IProgress<int>? progress = null, CancellationToken token = default)
        {
            var sfd = new SaveFileDialog { Filter = "PDF ファイル (*.pdf)|*.pdf", FileName = "calendar.pdf" };
            if (sfd.ShowDialog() != true) return;

            var images = new List<byte[]>(pages.Count);
            for (int i = 0; i < pages.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var png = RenderPageToPng(pages[i], PagePxW, PagePxH, Dpi);
                images.Add(png);
                progress?.Report((i + 1) * 50 / pages.Count);
            }

            token.ThrowIfCancellationRequested();

            var doc = Document.Create(container =>
            {
                foreach (var img in images)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(0);
                        page.Content().Element(e => e.AlignCenter().AlignMiddle().Image(img).FitArea());
                    });
                }
            });

            try { doc.GeneratePdf(sfd.FileName); }
            catch (Exception ex) { throw new InvalidOperationException("PDF生成に失敗しました。", ex); }

            progress?.Report(100);
        }

        // カレンダーページをWPFコントロールとして描画し、PNGバイト列へ変換。
        // PNG以外の形式にしたい場合はエンコーダーを差し替える。
        static byte[] RenderPageToPng(MonthPageViewModel vm, int w, int h, int dpi)
        {
            var control = new CalendarMaker.Views.MonthPageView
            {
                DataContext = vm,
                Width = PageSpec.PageDipW,
                Height = PageSpec.PageDipH,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            control.Measure(new System.Windows.Size(PageSpec.PageDipW, PageSpec.PageDipH));
            control.Arrange(new System.Windows.Rect(0, 0, PageSpec.PageDipW, PageSpec.PageDipH));
            control.UpdateLayout();

            var rtb = new RenderTargetBitmap(w, h, dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(control);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
}

