using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using CalendarMaker.ViewModels;

namespace CalendarMaker
{
    public partial class MainWindow : Window
    {
        public MainWindow() { InitializeComponent(); }
        private MainViewModel VM => (MainViewModel)DataContext;

        private void Rebuild_Click(object sender, RoutedEventArgs e) => VM.BuildAllPages();
        private void ExportPdf_Click(object sender, RoutedEventArgs e) => VM.ExportPdf();
        private void Print_Click(object sender, RoutedEventArgs e) => VM.Print();

        private void PickImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "画像ファイル|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|すべてのファイル|*.*"
                };
                if (dlg.ShowDialog() == true)
                    VM.Settings.SetMonthImagePath(index, dlg.FileName);
            }
        }

        private void AddAnniv_Click(object sender, RoutedEventArgs e)
        {
            if (AnnivDate.SelectedDate is DateTime dt && !string.IsNullOrWhiteSpace(AnnivLabel.Text))
            {
                VM.AddAnniversary(dt, AnnivLabel.Text.Trim());
                AnnivLabel.Text = "";
            }
        }
    }
}
