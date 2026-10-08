using System.Windows;
using Microsoft.JSInterop;

namespace AionDpsMeter.UI.Services.Windowing
{
    /// <summary>
    /// Esc pressed inside a Blazor page (Settings, the meter) never reaches WPF's keyboard events, so wwwroot/index.html
    /// forwards it here.
    /// </summary>
    public static class EscapeKeyBridge
    {
        [JSInvokable("CloseNewestWindowOnEscape")]
        public static Task CloseNewestWindowOnEscape() =>
            Application.Current.Dispatcher.InvokeAsync(() => AppWindow.CloseNewest()).Task;
    }
}
