using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Services.Input;
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
        public bool IsMeterEdit { get; private set; }
        public bool IsMoveKeyHeld => _moveKeyHeld;
        public bool IsSettingsOpen => _settingsOpen;

        private bool IsBuffOverlayEnabled { get; set; }
        private bool IsSkillCdOverlayEnabled { get; set; }

        // The meter and overlays accept the mouse while Settings is open or while the move key is held; otherwise they are click-through.
        private bool _settingsOpen;
        private PlayerDetailsWindow? _playerDetailsWindow;
        private bool _moveKeyHeld;

        private MainWindow MainWindow => serviceProvider.GetRequiredService<MainWindow>();

        private readonly IWindowManagerService windowManager;
        private readonly IServiceProvider serviceProvider;
        private readonly CombatSessionManager sessionManager;
        private readonly IAppSettingsService settingsService;
        private readonly UpdateCheckerService updateService;
        private readonly GameFocusWatcher focusWatcher;
        private readonly ModifierKeyWatcher moveKeyWatcher;

        // Default spots as fractions of the game window, until the user drags a window somewhere else.
        // The overlays are centered horizontally for their actual width; only their height is a fraction.
        private static readonly Point MeterDefault = new(0, 0.535);
        private const double OverlaysDefaultTop = 0.0868;
        private const double OverlayGap = 8;
        private static readonly WindowKey[] OverlayKeys = [WindowKey.BuffOverlay, WindowKey.SkillCdOverlay];
        private static readonly WindowKey[] ClickThroughKeys = [WindowKey.Main, .. OverlayKeys];

        public WindowHelper(IWindowManagerService windowManager, IServiceProvider serviceProvider, CombatSessionManager sessionManager, IAppSettingsService settingsService, UpdateCheckerService updateService, GameFocusWatcher focusWatcher, ModifierKeyWatcher moveKeyWatcher)
        {

            this.windowManager = windowManager;
            this.serviceProvider = serviceProvider;
            this.sessionManager = sessionManager;
            this.settingsService = settingsService;
            this.updateService = updateService;
            this.focusWatcher = focusWatcher;
            this.moveKeyWatcher = moveKeyWatcher;

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
            foreach (var key in ClickThroughKeys)
                windowManager.SetClickThrough(key);
            focusWatcher.GameFocused += (_, _) => PlaceWindowsOverGame();
            focusWatcher.GameMoved += (_, _) => PlaceWindowsOverGame();
            PlaceWindowsOverGame();

            moveKeyWatcher.HeldChanged += (_, _) =>
            {
                _moveKeyHeld = moveKeyWatcher.IsHeld;
                ApplyOverlayEditMode();
            };
            moveKeyWatcher.Start();
        }

        private void ApplyOverlayEditMode()
        {
            var editable = _settingsOpen || _moveKeyHeld;
            if (editable == IsMeterEdit) return;

            IsBuffEdit = editable;
            IsSkillCdEdit = editable;
            IsMeterEdit = editable;

            // Listeners may change window styles (e.g. the meter's resize grip), so click-through is applied last.
            WindowStateUpdated?.Invoke(this, EventArgs.Empty);
            foreach (var key in ClickThroughKeys)
            {
                if (editable) windowManager.RestoreClickThrough(key);
                else windowManager.SetClickThrough(key);
            }
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
            _settingsOpen = true;
            ApplyOverlayEditMode();
            var win = new BlazorWindow(App.AppHost.Services, typeof(SettingsPage))
            {
                Width = 500,
                Height = 900,
                EscapeClose = CloseSettings,
            };
            windowManager.Open(WindowKey.Settings, win, isSingleton: true, owner: MainWindow);
        }

        public void CloseSettings()
        {
            _settingsOpen = false;
            ApplyOverlayEditMode();
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
            var viewModel = new PlayerDetailsViewModel(
                sessionManager,
                player.PlayerId,
                player.PlayerNameDisplay,
                player.ClassName,
                null,
                player.ClassIcon,
                settingsService,
                player.CombatPower,
                player.ServerName);

            // One live details window: another player loads into it.
            if (_playerDetailsWindow is { } open)
            {
                (open.DataContext as PlayerDetailsViewModel)?.Dispose();
                open.DataContext = viewModel;
                windowManager.Focus(WindowKey.PlayerDetails);
                return;
            }

            var detailsWindow = new PlayerDetailsWindow
            {
                DataContext = viewModel,
                Owner = MainWindow
            };
            detailsWindow.Closed += (_, _) => _playerDetailsWindow = null;
            _playerDetailsWindow = detailsWindow;
            windowManager.Open(WindowKey.PlayerDetails, detailsWindow, true, null, MainWindow);
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
