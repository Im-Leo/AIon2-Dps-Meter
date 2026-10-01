using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Timed;
using AionDpsMeter.UI.Services.Windowing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;


namespace AionDpsMeter.UI.Pages
{
    partial class BuffOverlay(IWindowManagerService windowManager, WindowHelper windowHelper, IAppSettingsService appSettingsService, [FromKeyedServices("Buffs")] ITimedEventTracker buffTracker)
    {

        private bool IsEditable => windowHelper.IsBuffEdit;
        private int Count => buffTracker.Items.Count;
        private CancellationTokenSource? timerCts;
        private OverlaySettings settings = new();

        private IReadOnlyList<TimedItemState> displayedItems = Array.Empty<TimedItemState>();
       
        protected override void OnInitialized()
        {
            settings = appSettingsService.BufOverlaySettings;

            appSettingsService.SettingsChanged += OnSettingsChanged;

            timerCts = new CancellationTokenSource();
            _ = RunRefreshTimerAsync(timerCts.Token);
        }

   

        private async Task RunRefreshTimerAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var currentItems = buffTracker.Items.ToList();

                    var newItems = settings.Order == OverlayOrderMode.Ascending
                        ? currentItems.OrderBy(i => i.TimeLeft).ToList()
                        : currentItems.OrderByDescending(i => i.TimeLeft).ToList();

                    await InvokeAsync(() =>
                    {
                        displayedItems = newItems;
                        StateHasChanged();
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
        }



        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            settings = appSettingsService.BufOverlaySettings;
            InvokeAsync(StateHasChanged);
        }

        private void BeginDrag(MouseEventArgs _)
        {
            if (!IsEditable) return;
            windowManager.Drag(WindowKey.BuffOverlay);
        }

      

        private string ContainerStyle
        {
            get
            {
                if (!IsEditable) return string.Empty;

                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var minHeight = settings.IconSize + 5;
                var minWidth = (settings.IconSize + 5) * 10;
                return $"min-height: {minHeight.ToString(culture)}px; min-width: {minWidth.ToString(culture)}px;";
            }
        }

        private string TrackStyle => "gap: 4px;";

        private string IconStyle =>
            $"width: {settings.IconSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px; " +
            $"height: {settings.IconSize.ToString(System.Globalization.CultureInfo.InvariantCulture)}px;";

        private string CdTextStyle =>
            $"font-size: {(settings.IconSize / 3).ToString("0", System.Globalization.CultureInfo.InvariantCulture)}px";
        private static string FormatTimeLeft(TimeSpan timeLeft)
        {
            var seconds = Math.Max(0, timeLeft.TotalSeconds);
            return seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }


        public void Dispose()
        {
            appSettingsService.SettingsChanged -= OnSettingsChanged;
            timerCts?.Cancel();
            timerCts?.Dispose();
            timerCts = null;
        }
    }
}
