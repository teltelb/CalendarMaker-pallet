namespace CalendarMaker
{
    /// <summary>
    /// 用紙サイズや解像度を調整したいときはこのクラスを変更します。
    /// エクスポート／印刷の仕様をまとめて管理する拠点です。
    /// </summary>
    public static class PageSpec
    {
        // PdfExporter などが使用する基準DPI。数値を上げると画質↑・ファイルサイズ↑。
        public const int PdfDpi = 300;

        // A4（300dpi）想定のピクセルサイズ。別の用紙にしたいときは幅・高さをセットで変更。
        public const int PagePxW = 2480;
        public const int PagePxH = 3508;

        // WPFの見た目（96dpi換算）とピクセル値の整合を取るための計算結果。
        public static readonly double PageDipW = PagePxW * 96.0 / PdfDpi;
        public static readonly double PageDipH = PagePxH * 96.0 / PdfDpi;
    }
}
