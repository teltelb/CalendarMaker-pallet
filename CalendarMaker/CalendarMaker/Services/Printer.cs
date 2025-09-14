using System.Collections.Generic;
using System.Windows;
using CalendarMaker.ViewModels;

namespace CalendarMaker.Services
{
    public static class Printer
    {
        public static void Print(IList<MonthPageViewModel> pages)
        {
            var dlg = new System.Windows.Controls.PrintDialog();
            if (dlg.ShowDialog() != true) return;

            foreach (var vm in pages)
            {
                var control = new CalendarMaker.Views.MonthPageView { DataContext = vm };
                control.Measure(new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight));
                control.Arrange(new System.Windows.Rect(new Point(0, 0), new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight)));
                control.UpdateLayout();
                dlg.PrintVisual(control, $"{vm.Year}-{vm.Month}");
            }
        }
    }
}
