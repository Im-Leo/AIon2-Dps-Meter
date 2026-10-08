using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Timed;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components.Web;

namespace AionDpsMeter.UI.Pages
{
    partial class TimersOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService appSettingsService, RiftTracker riftTracker)
    {
        private bool IsEditable => windowHelper.IsTimersEdit;
        private CancellationTokenSource? timerCts;
        private TimersOverlaySettings overlaySettings = new();
        private SpacetimeRiftSettings riftSettings = new();

        private string clock = string.Empty;
        private string riftLabel = string.Empty;
        private string riftCountdown = string.Empty;
        private string riftClass = string.Empty;

        // Color steps as time runs out: open to enter, then inside the rift.
        private static readonly TimeSpan OpenWarn = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan OpenUrgent = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan InsideWarn = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan InsideUrgent = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan Blink = TimeSpan.FromMinutes(1);

        protected override void OnInitialized()
        {
            LoadSettings();
            Refresh();
            appSettingsService.SettingsChanged += OnSettingsChanged;
            windowHelper.WindowStateUpdated += OnEditModeChanged;

            timerCts = new CancellationTokenSource();
            _ = RunRefreshTimerAsync(timerCts.Token);
        }

        private async Task RunRefreshTimerAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    await InvokeAsync(() =>
                    {
                        Refresh();
                        StateHasChanged();
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void Refresh()
        {
            var nowUtc = DateTime.UtcNow;
            clock = nowUtc.ToLocalTime().ToString(overlaySettings.Use24HourClock ? "HH:mm:ss" : "h:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture);

            var rift = riftTracker.GetState(nowUtc);
            if (rift.UntilUtc is not { } untilUtc)
            {
                riftCountdown = string.Empty;
                return;
            }

            var remaining = untilUtc - nowUtc;
            riftCountdown = $"{(int)remaining.TotalHours}:{remaining:mm\\:ss}";
            string blink = remaining <= Blink ? " is-blink" : string.Empty;
            (riftLabel, riftClass) = rift.Phase switch
            {
                RiftTracker.RiftPhase.Open => ("Rift open", (remaining <= OpenUrgent ? "is-urgent" : remaining <= OpenWarn ? "is-warn" : "is-open") + blink),
                RiftTracker.RiftPhase.Inside => ("In rift", (remaining <= InsideUrgent ? "is-urgent" : remaining <= InsideWarn ? "is-warn" : "is-inside") + blink),
                _ => ("Spacetime Rift", remaining <= TimeSpan.FromMinutes(riftSettings.LeadMinutes) ? "is-warn is-blink" : string.Empty),
            };
        }

        private void LoadSettings()
        {
            overlaySettings = appSettingsService.TimersOverlaySettings;
            riftSettings = appSettingsService.SpacetimeRiftSettings;
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            LoadSettings();
            InvokeAsync(StateHasChanged);
        }

        private void OnEditModeChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.TimersOverlay);
        }

        public void Dispose()
        {
            appSettingsService.SettingsChanged -= OnSettingsChanged;
            windowHelper.WindowStateUpdated -= OnEditModeChanged;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }
    }
}
