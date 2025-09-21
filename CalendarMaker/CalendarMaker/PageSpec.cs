namespace CalendarMaker
{
    public static class PageSpec
    {
        public const int PdfDpi = 300;
        public const int PagePxW = 2480; // A4 300dpi
        public const int PagePxH = 3508;
        public static readonly double PageDipW = PagePxW * 96.0 / PdfDpi;
        public static readonly double PageDipH = PagePxH * 96.0 / PdfDpi;
    }
}
