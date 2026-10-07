using System.Diagnostics;
using AionDpsMeter.UI.Services.Windowing.Native;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.UI.Services.Windowing
{
    public enum ForegroundKind { Other, Game, Self }

    /// <summary>
    /// Tracks whether the foreground window belongs to the game, to this app, or to something else.
    /// Purely event-driven: the foreground WinEvent.
    /// </summary>
    public sealed class GameFocusWatcher(ILogger<GameFocusWatcher> logger) : IDisposable
    {
        private const string GameProcessName = "AION2";

        private readonly int _ownProcessId = Environment.ProcessId;

        // Held in a field: the native hook calls this delegate, so it must outlive the hook.
        private NativeMethods.WinEventProc? _callback;
        private IntPtr _hook;

        private uint _lastProcessId;
        private bool _lastProcessIsGame;
        private int _gameProcessId;

        public ForegroundKind Foreground { get; private set; } = ForegroundKind.Other;

        public event EventHandler? ForegroundChanged;

        public void Start()
        {
            if (_hook != IntPtr.Zero) return;

            Evaluate(NativeMethods.GetForegroundWindow());
            IsGameRunning();
            _callback = OnForegroundChanged;
            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _callback, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
            if (_hook == IntPtr.Zero)
                logger.LogWarning("Foreground hook could not be installed; game focus detection is inactive.");
        }

        public bool IsGameRunning()
        {
            if (_gameProcessId != 0)
            {
                try
                {
                    using var cached = Process.GetProcessById(_gameProcessId);
                    if (!cached.HasExited) return true;
                }
                catch (ArgumentException)
                {
                    // Exited since the last check; fall through to a fresh lookup.
                }

                _gameProcessId = 0;
            }

            var running = Process.GetProcessesByName(GameProcessName);
            try
            {
                var withWindow = running.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero) ?? running.FirstOrDefault();
                _gameProcessId = withWindow?.Id ?? 0;
                return withWindow is not null;
            }
            finally
            {
                foreach (var process in running) process.Dispose();
            }
        }

        // Events can arrive late or out of order, so the current foreground is read instead of trusting the event's window:
        // a stale event for one of the app's own windows would otherwise leave it believing it is in front.
        private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime) =>
            Evaluate(NativeMethods.GetForegroundWindow());

        private void Evaluate(IntPtr hWnd)
        {
            // A null foreground window is a transient state during focus switches.
            if (hWnd == IntPtr.Zero) return;

            var kind = Classify(hWnd);
            if (kind == Foreground) return;

            Foreground = kind;
            ForegroundChanged?.Invoke(this, EventArgs.Empty);
        }

        private ForegroundKind Classify(IntPtr hWnd)
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0) return ForegroundKind.Other;
            if (processId == _ownProcessId) return ForegroundKind.Self;
            if (processId == _lastProcessId) return _lastProcessIsGame ? ForegroundKind.Game : ForegroundKind.Other;

            string processName;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                logger.LogDebug(ex, "Foreground process {ProcessId} could not be resolved", processId);
                return ForegroundKind.Other;
            }

            var isGame = string.Equals(processName, GameProcessName, StringComparison.OrdinalIgnoreCase);
            _lastProcessId = processId;
            _lastProcessIsGame = isGame;
            if (isGame) _gameProcessId = (int)processId;
            logger.LogDebug("Foreground: {ProcessName} ({ProcessId}) isGame={IsGame}", processName, processId, isGame);
            return isGame ? ForegroundKind.Game : ForegroundKind.Other;
        }

        public void Dispose()
        {
            if (_hook == IntPtr.Zero) return;
            NativeMethods.UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
