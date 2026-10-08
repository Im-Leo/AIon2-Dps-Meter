using AionDpsMeter.UI.Services.Tray;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AionDpsMeter.UI
{
    /// <summary>
    /// Entry point. Allows a single running meter: the check runs before anything of the app is loaded (data, logging,
    /// game data, packet capture, tray, windows). A second launch only asks the running meter to show itself, then exits.
    /// </summary>
    public static class Program
    {
        // Local: one meter per Windows session.
        private const string InstanceMutexName = @"Local\Aion2DPSMeter.SingleInstance";
        private const string ShowEventName = @"Local\Aion2DPSMeter.Show";

        [STAThread]
        public static void Main()
        {
            using var instance = new Mutex(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);
            using var showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            if (!isFirstInstance)
            {
                showRequest.Set();
                return;
            }

            var showWait = ThreadPool.RegisterWaitForSingleObject(showRequest, (_, _) => ShowRunningInstance(), null, Timeout.Infinite, executeOnlyOnce: false);
            try
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            finally
            {
                showWait.Unregister(null);
            }
        }

        private static void ShowRunningInstance() =>
            Application.Current?.Dispatcher.InvokeAsync(() => App.AppHost?.Services.GetService<TrayService>()?.Restore());
    }
}
