using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.ViewModels;
using AionDpsMeter.UI.ViewModels.History;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class HistorySessionWindow : AppWindow
    {
        private readonly IAppSettingsService _settingsService;
        private PlayerDetailsWindow? _detailsWindow;

        public HistorySessionWindow(IAppSettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            // The details shown belong to the previous encounter once another one is loaded.
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is not null) _detailsWindow?.Close();
            };
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el &&
                el.Tag is HistoryPlayerViewModel player &&
                DataContext is HistorySessionViewModel session)
            {
                var vm = PlayerDetailsViewModel.FromSnapshot(player, _settingsService, session.TargetName);

                // One details window per encounter window: another player loads into it.
                if (_detailsWindow is { } open)
                {
                    (open.DataContext as PlayerDetailsViewModel)?.Dispose();
                    open.DataContext = vm;
                    open.Activate();
                    return;
                }

                var detailsWindow = new PlayerDetailsWindow
                {
                    DataContext = vm,
                    Owner = this
                };
                detailsWindow.Closed += (_, _) => _detailsWindow = null;
                _detailsWindow = detailsWindow;
                detailsWindow.Show();
            }
        }
    }
}
