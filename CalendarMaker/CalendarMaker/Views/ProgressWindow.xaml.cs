using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace CalendarMaker.Views
{
    public partial class ProgressWindow : Window, INotifyPropertyChanged
    {
        private int _progress;
        public int Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }

        private string _statusText = "準備中...";
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;

        public IProgress<int> Reporter { get; }

        public ProgressWindow()
        {
            InitializeComponent();
            DataContext = this;
            Reporter = new Progress<int>(p => { Progress = p; StatusText = $"出力中... {p}%"; });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            StatusText = "キャンセルしています...";
            IsEnabled = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
