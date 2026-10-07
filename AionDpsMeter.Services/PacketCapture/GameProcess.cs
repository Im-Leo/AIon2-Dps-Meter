using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AionDpsMeter.Services.PacketCapture
{
    /// <summary>
    /// Ties captured connections to the game process, so detection never locks onto another program's traffic
    /// (for example HTTPS that happens to contain the heartbeat bytes while the game is closed).
    /// </summary>
    internal static class GameProcess
    {
        private const string ProcessName = "AION2";
        private const long RunningCacheMs = 2000;

        private static long checkedAt = -RunningCacheMs;
        private static bool running;

        public static bool IsRunning()
        {
            long now = Environment.TickCount64;
            if (now - Volatile.Read(ref checkedAt) < RunningCacheMs) return running;

            var processes = Process.GetProcessesByName(ProcessName);
            running = processes.Length > 0;
            foreach (var p in processes) p.Dispose();
            Volatile.Write(ref checkedAt, now);
            return running;
        }

        /// <summary>
        /// Name of the process that owns the local IPv4 TCP endpoint on <paramref name="localPort"/>, or null when not found.
        /// </summary>
        public static string? OwnerOfLocalPort(int localPort)
        {
            int size = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref size, false, AfInet, TcpTableOwnerPidAll, 0);
            IntPtr table = Marshal.AllocHGlobal(size);
            try
            {
                // The table can grow between the size query and the read; the call then reports the new size.
                uint result;
                while ((result = GetExtendedTcpTable(table, ref size, false, AfInet, TcpTableOwnerPidAll, 0)) == ErrorInsufficientBuffer)
                    table = Marshal.ReAllocHGlobal(table, size);
                if (result != 0) return null;

                int count = Marshal.ReadInt32(table);
                int rowSize = Marshal.SizeOf<TcpRowOwnerPid>();
                for (int i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<TcpRowOwnerPid>(table + 4 + i * rowSize);
                    // Ports are stored in network byte order in the low 16 bits.
                    int port = (int)(((row.LocalPort & 0xFF) << 8) | ((row.LocalPort >> 8) & 0xFF));
                    if (port != localPort) continue;

                    try
                    {
                        using var process = Process.GetProcessById((int)row.OwningPid);
                        return process.ProcessName;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(table);
            }
        }

        public static bool IsGameProcess(string? name) => string.Equals(name, ProcessName, StringComparison.OrdinalIgnoreCase);

        private const int AfInet = 2;
        private const int TcpTableOwnerPidAll = 5;
        private const uint ErrorInsufficientBuffer = 122;

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddr;
            public uint LocalPort;
            public uint RemoteAddr;
            public uint RemotePort;
            public uint OwningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int ipVersion, int tableClass, uint reserved);
    }
}
