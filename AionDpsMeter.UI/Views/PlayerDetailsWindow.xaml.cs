using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    /// <summary>
    /// Interaction logic for PlayerDetailsWindow.xaml
    /// </summary>
    public partial class PlayerDetailsWindow : AppWindow
    {
        public PlayerDetailsWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PlayerDetailsViewModel viewModel)
            {
                viewModel.Dispose();
            }
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is PlayerDetailsViewModel viewModel)
            {
                viewModel.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
