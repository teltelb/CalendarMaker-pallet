using CalendarMaker.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CalendarMaker.Services
{
    public static class PdfExporter
    {
        const int Dpi = 300;
        const int PagePxW = 2480; // A4 300dpi
        const int PagePxH = 3508;

        public static void Export(IList<MonthPageViewModel> pages)
        {
            var doc = Document.Create(container =>
            {
                foreach (var vm in pages)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(0);
                        page.Content().Image(RenderPageToPng(vm, PagePxW, PagePxH, Dpi));
                    });
                }
            });

            var path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                "calendar.pdf");

            doc.GeneratePdf(path);
            MessageBox.Show($"PDFを書き出しました:\n{path}");
        }

        static byte[] RenderPageToPng(MonthPageViewModel vm, int w, int h, int dpi)
        {
            var control = new CalendarMaker.Views.MonthPageView { DataContext = vm, Width = w, Height = h };
            control.Measure(new System.Windows.Size(w, h));
            control.Arrange(new System.Windows.Rect(0, 0, w, h));
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
