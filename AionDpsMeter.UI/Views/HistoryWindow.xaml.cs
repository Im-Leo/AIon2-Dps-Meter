using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels.History;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class HistoryWindow : AppWindow
    {
        private readonly CombatSessionManager _sessionManager;
        private readonly IAppSettingsService _settingsService;
        private HistorySessionWindow? _sessionWindow;

        public HistoryWindow(CombatSessionManager sessionManager, IAppSettingsService settingsService)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _settingsService = settingsService;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PositionWindowToRight(Window child)
        {
            const double gap = 8;

            var wa = ScreenHelper.GetWorkingAreaForWindow(this);

            double parentRight = Left + Width;
            double candidateLeft = parentRight + gap;

            double childLeft = candidateLeft + child.Width <= wa.Right
                ? candidateLeft
                : Math.Max(wa.Left, wa.Right - child.Width);

            double childTop = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - child.Height));

            child.WindowStartupLocation = WindowStartupLocation.Manual;
            child.Left = childLeft;
            child.Top = childTop;
        }

        private void SessionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is HistoryEntryViewModel entry)
            {
                var snapshot = _sessionManager.GetHistorySession(entry.SessionId);
                if (snapshot is null) return;

                var viewModel = new HistorySessionViewModel(snapshot, _settingsService);

                // One encounter window: another encounter loads into it, keeping the position the user gave it.
                if (_sessionWindow is { } open)
                {
                    open.DataContext = viewModel;
                    open.Activate();
                    return;
                }

                var sessionWindow = new HistorySessionWindow(_settingsService)
                {
                    DataContext = viewModel,
                    Owner = this
                };
                sessionWindow.Closed += (_, _) => _sessionWindow = null;
                _sessionWindow = sessionWindow;

                PositionWindowToRight(sessionWindow);
                sessionWindow.Show();
            }
        }
    }
}
