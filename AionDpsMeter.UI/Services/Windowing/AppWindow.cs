using System.Windows;
using System.Windows.Interop;
using AionDpsMeter.UI.Services.Windowing.Native;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Base for every app window: kept out of Alt+Tab and the taskbar from the moment its handle exists, however it is
    /// shown later. The app is reached through its own windows and the tray icon.
    /// </summary>
    public class AppWindow : Window
    {
        public AppWindow()
        {
            ShowInTaskbar = false;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            long style = NativeWindowHelper.GetExtendedStyle(hwnd);
            NativeWindowHelper.SetExtendedStyle(hwnd, (style | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW);
        }
    }
}
