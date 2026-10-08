using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using AionDpsMeter.UI.Services.Windowing.Native;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Base for every app window: kept out of Alt+Tab and the taskbar from the moment its handle exists, however it is
    /// shown later. The app is reached through its own windows and the tray icon. Esc in any app window closes the most
    /// recently shown window that allows it.
    /// </summary>
    public class AppWindow : Window
    {
        // Ordered by when each window was last shown, newest last.
        private static readonly List<AppWindow> ShownOrder = new();

        /// <summary>Set while hidden windows are brought back (alt-tab into the game): that is not the user opening them.</summary>
        public static bool RestoringVisibility { get; set; }

        /// <summary>False for windows that must stay open (the meter and the overlays).</summary>
        public bool ClosesOnEscape { get; set; } = true;

        /// <summary>How Esc closes this window, when it is not a plain <see cref="Window.Close"/> (Settings hides instead).</summary>
        public Action? EscapeClose { get; set; }

        public AppWindow()
        {
            ShowInTaskbar = false;
            IsVisibleChanged += (_, e) =>
            {
                if (!(bool)e.NewValue || RestoringVisibility) return;
                ShownOrder.Remove(this);
                ShownOrder.Add(this);
            };
        }

        /// <summary>Closes the most recently shown window that allows it. Returns false when there is none.</summary>
        public static bool CloseNewest()
        {
            var window = ShownOrder.LastOrDefault(w => w.ClosesOnEscape && w.IsVisible);
            if (window is null) return false;

            if (window.EscapeClose is { } close) close();
            else window.Close();

            // Windows may hand the foreground to the game when a window closes; the next Esc must reach the next window in line.
            ShownOrder.LastOrDefault(w => w.ClosesOnEscape && w.IsVisible)?.Activate();
            return true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            long style = NativeWindowHelper.GetExtendedStyle(hwnd);
            NativeWindowHelper.SetExtendedStyle(hwnd, (style | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && CloseNewest())
                e.Handled = true;
            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ShownOrder.Remove(this);
            base.OnClosed(e);
        }
    }
}
