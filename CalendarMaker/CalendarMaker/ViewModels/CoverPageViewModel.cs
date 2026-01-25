using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CalendarMaker.Models;

namespace CalendarMaker.ViewModels
{
    public class CoverPageViewModel : ICalendarPageViewModel
    {
        public string DisplayName => "表紙";

        public bool UseFixedHorizontalSpecLayout { get; set; }

        public double PageDipW { get; set; }
        public double PageDipH { get; set; }

        public string Title { get; set; } = "CALENDAR";
        public string Subtitle { get; set; } = string.Empty;
        public Brush TextBrush { get; set; } = Brushes.Black;

        // Legacy layout (portrait, etc.)
        public CoverLayoutType LayoutType { get; set; } = CoverLayoutType.Grid3x4;

        // Fixed horizontal spec layout
        public ObservableCollection<CoverDecorLineViewModel> DecorLines { get; set; } = new();
        public double TitleTop { get; set; }
        public double SubtitleTop { get; set; }
        public double TitleBoxHeight { get; set; }
        public double SubtitleBoxHeight { get; set; }
        public double TitleFontSize { get; set; }
        public double SubtitleFontSize { get; set; }

        public ObservableCollection<CoverTileViewModel> Tiles { get; set; } = new();
    }

    public class CoverDecorLineViewModel
    {
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y { get; set; }
        public double StrokeThickness { get; set; }
    }

    public class CoverTileViewModel
    {
        public string? ImagePath { get; set; }

        // Legacy: use stored 3:2 crop for preview-like collage.
        public Rect ImageCropRect { get; set; } = new(0, 0, 1, 1);
        public string Label { get; set; } = string.Empty;

        // Fixed horizontal spec: absolute placement on the page.
        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }
    }
}

