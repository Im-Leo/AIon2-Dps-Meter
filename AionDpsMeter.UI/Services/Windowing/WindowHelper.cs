using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels;
using AionDpsMeter.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AionDpsMeter.UI.Services.Windowing
{
    public class WindowHelper
    {
        //window states
        public EventHandler? WindowStateUpdated { get; set; }
        public bool IsBuffEdit { get; private set; }
        public bool IsSkillCdEdit { get; private set; }

        private bool IsBuffOverlayEnabled { get; set; }
        private bool IsSkillCdOverlayEnabled { get; set; }

        private MainWindow MainWindow => serviceProvider.GetRequiredService<MainWindow>();

        private readonly IWindowManagerService windowManager;
        private readonly IServiceProvider serviceProvider;
        private readonly CombatSessionManager sessionManager;
        private readonly IAppSettingsService settingsService;
        private readonly UpdateCheckerService updateService;
        private readonly GameFocusWatcher focusWatcher;

        // Default spots as fractions of the game window, until the user drags a window somewhere else.
        // The overlays are centered horizontally for their actual width; only their height is a fraction.
        private static readonly Point MeterDefault = new(0, 0.535);
        private const double OverlaysDefaultTop = 0.0868;
        private const double OverlayGap = 8;
        private static readonly WindowKey[] OverlayKeys = [WindowKey.BuffOverlay, WindowKey.SkillCdOverlay];

        public WindowHelper(IWindowManagerService windowManager, IServiceProvider serviceProvider, CombatSessionManager sessionManager, IAppSettingsService settingsService, UpdateCheckerService updateService, GameFocusWatcher focusWatcher)
        {

            this.windowManager = windowManager;
            this.serviceProvider = serviceProvider;
            this.sessionManager = sessionManager;
            this.settingsService = settingsService;
            this.updateService = updateService;
            this.focusWatcher = focusWatcher;

            IsBuffOverlayEnabled = settingsService.BufOverlaySettings.Enabled;
            IsSkillCdOverlayEnabled = settingsService.SkillCdOverlaySettings.Enabled;
            settingsService.SettingsChanged += SettingsChanged;
        }

        private void SettingsChanged(object? sender, EventArgs e)
        {
            if (IsBuffOverlayEnabled != settingsService.BufOverlaySettings.Enabled)
            {
                IsBuffOverlayEnabled = settingsService.BufOverlaySettings.Enabled;
                ManageBuffOverlay();
            }

            if (IsSkillCdOverlayEnabled != settingsService.SkillCdOverlaySettings.Enabled)
            {
                IsSkillCdOverlayEnabled = settingsService.SkillCdOverlaySettings.Enabled;
                ManageSkillCdOverlay();
            }
                
        }

        public void OpenRequiredWindows()
        {
            ManageBuffOverlay();
            ManageSkillCdOverlay();
            windowManager.SetClickThrough(WindowKey.BuffOverlay);
            windowManager.SetClickThrough(WindowKey.SkillCdOverlay);
            focusWatcher.GameFocused += (_, _) => PlaceWindowsOverGame();
            focusWatcher.GameMoved += (_, _) => PlaceWindowsOverGame();
            PlaceWindowsOverGame();
        }

        /// <summary>
        /// The meter and overlays always sit over the game window, wherever it is: at their saved game-relative spot,
        /// else at the default spots.
        /// </summary>
        private void PlaceWindowsOverGame()
        {
            var game = focusWatcher.FindGameWindow();
            if (game == IntPtr.Zero || ScreenHelper.GetWindowRectDips(game, MainWindow) is not { } gameRect) return;

            Point At(Point fraction) => new(gameRect.Left + fraction.X * gameRect.Width, gameRect.Top + fraction.Y * gameRect.Height);

            windowManager.PlaceOverGame(WindowKey.Main, gameRect, _ => At(MeterDefault));

            // The buff and skill-cooldown overlays stack near the top, centered.
            var top = gameRect.Top + OverlaysDefaultTop * gameRect.Height;
            foreach (var key in OverlayKeys)
            {
                var overlayTop = top;
                var size = windowManager.PlaceOverGame(key, gameRect,
                    s => new Point(gameRect.Left + (gameRect.Width - s.Width) / 2, overlayTop));
                if (!size.IsEmpty) top += size.Height + OverlayGap;
            }
        }


        public void OpenSettings()
        {
            IsBuffEdit = true;
            IsSkillCdEdit = true;
            windowManager.RestoreClickThrough(WindowKey.BuffOverlay);
            windowManager.RestoreClickThrough(WindowKey.SkillCdOverlay);
            WindowStateUpdated?.Invoke(this, EventArgs.Empty);
            var win = new BlazorWindow(App.AppHost.Services, typeof(SettingsPage))
            {
                Width = 500,
                Height = 900,
            };
            windowManager.Open(WindowKey.Settings, win, isSingleton: true, owner: MainWindow);
        }

        public void CloseSettings()
        {
            IsBuffEdit = false;
            IsSkillCdEdit = false;
            windowManager.SetClickThrough(WindowKey.BuffOverlay);
            windowManager.SetClickThrough(WindowKey.SkillCdOverlay);
            WindowStateUpdated?.Invoke(this, EventArgs.Empty);
            windowManager.Hide(WindowKey.Settings);
        }
        public void OpenHistory()
        {
            var historyWindow = new HistoryWindow(sessionManager, settingsService)
            {
                DataContext = new ViewModels.History.HistoryViewModel(sessionManager, settingsService),
                Owner = MainWindow
            };
            windowManager.Open(WindowKey.History, historyWindow, true, null, MainWindow);
        }

        public void OpenStatEff()
        {
            var statEfficiencyCalculatorViewModel  =new StatEfficiencyCalculatorViewModel(settingsService);
            statEfficiencyCalculatorViewModel.LoadFromSnapshot(sessionManager.GetCurrentPlayerStatSnapshot());
            var statEfficiencyCalculatorWindow = new StatEfficiencyCalculatorWindow
            {
                DataContext = statEfficiencyCalculatorViewModel,
                Owner = MainWindow,
            };

            windowManager.Open(WindowKey.StatEfficiencyCalculator, statEfficiencyCalculatorWindow, true, null, MainWindow);
        }

        public void OpenWhatsNewWindow()
        {
            if (updateService.LatestReleaseInfo == null) return;
            var win = new WhatsNewWindow(updateService.LatestReleaseInfo, updateService)
            {
                Owner =  MainWindow
            };
            windowManager.Open(WindowKey.WhatsNew, win, true, null, MainWindow);
        }


        public void OpenPlayerDetails(PlayerRenderState player)
        {

            var detailsWindow = new PlayerDetailsWindow
            {
                DataContext = new PlayerDetailsViewModel(
                    sessionManager,
                    player.PlayerId,
                    player.PlayerNameDisplay,
                    player.ClassName,
                    null,
                    player.ClassIcon,
                    settingsService,
                    player.CombatPower,
                    player.ServerName),
                Owner = MainWindow
            };
            windowManager.Open(WindowKey.PlayerDetails, detailsWindow, false, null, MainWindow);
        }


        private void ManageBuffOverlay()
        {
            if (IsBuffOverlayEnabled) OpenBuffOverlay();
            else HideBuffOverlay();
        }
        private void ManageSkillCdOverlay()
        {
            if (IsSkillCdOverlayEnabled) OpenSkillCdOverlay();
            else HideSkillCdOverlay();
        }

        private void OpenBuffOverlay()
        {
            var buffOverlay = new BuffOverlayWindow();
            windowManager.Open(WindowKey.BuffOverlay, buffOverlay, true);
        }

        private void HideBuffOverlay()
        {
            windowManager.Hide(WindowKey.BuffOverlay);
        }

        private void OpenSkillCdOverlay()
        {
            var skillCdOverlay = new SkillCdOverlayWindow();
            windowManager.Open(WindowKey.SkillCdOverlay, skillCdOverlay, true);
        }

        private void HideSkillCdOverlay()
        {
            windowManager.Hide(WindowKey.SkillCdOverlay);
        }
    }
}
