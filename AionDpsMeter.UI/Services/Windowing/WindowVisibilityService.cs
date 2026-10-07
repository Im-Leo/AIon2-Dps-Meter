using System.Windows;

namespace AionDpsMeter.UI.Services.Windowing
{
    [Flags]
    public enum HideReason
    {
        None = 0,
        Tray = 1
    }

    /// <summary>
    /// Hides all app windows while any <see cref="HideReason"/> applies and shows them again once none does.
    /// </summary>
    public sealed class WindowVisibilityService
    {
        private readonly List<Window> _hiddenWindows = new();
        private HideReason _reasons;

        public bool IsHiddenBy(HideReason reason) => (_reasons & reason) != 0;

        public bool IsHidden => _reasons != HideReason.None;

        public void Hide(HideReason reason)
        {
            if (_reasons == HideReason.None)
            {
                _hiddenWindows.Clear();
                _hiddenWindows.AddRange(Application.Current.Windows.OfType<Window>().Where(w => w.IsVisible));
                foreach (var window in _hiddenWindows)
                    window.Hide();
            }

            _reasons |= reason;
        }

        /// <summary>
        /// Clears one reason. Windows reappear without taking focus, so the game keeps keyboard input.
        /// </summary>
        public void Clear(HideReason reason)
        {
            if (!IsHiddenBy(reason)) return;

            _reasons &= ~reason;
            if (_reasons == HideReason.None)
                ShowHiddenWindows();
        }

        /// <summary>
        /// Explicit user restore: clears every reason and focuses the main window.
        /// </summary>
        public void RestoreAll()
        {
            if (_reasons != HideReason.None)
            {
                _reasons = HideReason.None;
                ShowHiddenWindows();
            }

            Application.Current.MainWindow?.Activate();
        }

        private void ShowHiddenWindows()
        {
            var openWindows = Application.Current.Windows.OfType<Window>().ToHashSet();
            foreach (var window in _hiddenWindows.Where(openWindows.Contains))
            {
                var showActivated = window.ShowActivated;
                window.ShowActivated = false;
                window.Show();
                window.ShowActivated = showActivated;

                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
            }

            _hiddenWindows.Clear();
        }
    }
}
