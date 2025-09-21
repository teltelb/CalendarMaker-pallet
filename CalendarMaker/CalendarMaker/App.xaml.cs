using System.Windows;
using QuestPDF.Infrastructure; // QuestPDF を使う場合

namespace CalendarMaker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // QuestPDFのライセンス。商用条件に合わせて切替え
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }
}
