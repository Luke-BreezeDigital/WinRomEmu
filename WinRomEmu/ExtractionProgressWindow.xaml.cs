using System.ComponentModel;
using System.Windows;

namespace WinRomEmu
{
    public partial class ExtractionProgressWindow : Window
    {
        public double Percentage { get; set; }
        public ExtractionProgressWindow()
        {
            InitializeComponent();

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(Percentage < 100)
            {
                if(MessageBox.Show("Closing this window will cancel the process. Are you sure you wish to continue?", "Process Cancellation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                } else
                {
                    Application.Current.Shutdown();
                }
            }

            base.OnClosing(e);
        }

        public void UpdateProgress(double percentage, string currentFile)
        {
            Percentage = percentage;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateProgress(percentage, currentFile));
                return;
            }

            ProgressBar.Value = percentage;
            ProgressText.Text = $"{percentage.ToString("0.00")}%";
            StatusText.Text = $"Extracting: {currentFile}";
        }
    }
}