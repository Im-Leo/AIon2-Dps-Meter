namespace AionDpsMeter.Core
{
    /// <summary>
    /// Where user data lives: %LOCALAPPDATA%\Aion2DPSMeter, so settings, history, logs and caches survive deleting or
    /// replacing the program folder. Program files (game data, appsettings.json, the updater) stay next to the executable.
    /// </summary>
    public static class AppPaths
    {
        public const string DataFolderName = "Aion2DPSMeter";

        private const string SettingsFileName = "appsettings.user.json";
        private const string HistoryDatabaseFileName = "combat-history.db";
        private const string LogsFolderName = "Logs";
        private const string IconCacheFolderName = "IconCache";
        private const string PacketLogsFolderName = "PacketLogs";

        // The environment variable is read first so it matches the %LOCALAPPDATA% path used for logs in appsettings.json.
        public static string DataDirectory { get; } = Path.Combine(
            Environment.GetEnvironmentVariable("LOCALAPPDATA") is { Length: > 0 } localAppData
                ? localAppData
                : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DataFolderName);

        public static string SettingsFile => Path.Combine(DataDirectory, SettingsFileName);
        public static string HistoryDatabase => Path.Combine(DataDirectory, HistoryDatabaseFileName);
        public static string IconCacheDirectory => Path.Combine(DataDirectory, IconCacheFolderName, "Skills");
        public static string PacketLogsDirectory => Path.Combine(DataDirectory, PacketLogsFolderName);

        /// <summary>Skill and buff icons shipped with the app (tools/SkillIconPacker), served to the Blazor pages from wwwroot too.</summary>
        public static string SkillIconPackDirectory => Path.Combine(AppContext.BaseDirectory, "wwwroot", "icons", "skills");

        /// <summary>
        /// Copies user data left in the program folder by earlier versions. Only what is missing in the data folder is
        /// copied; nothing is overwritten or removed.
        /// </summary>
        public static void CopyLegacyData(string programDirectory)
        {
            Directory.CreateDirectory(DataDirectory);

            CopyFileIfMissing(programDirectory, SettingsFileName);

            // The database and its write-ahead log are one unit: recent sessions live in the -wal file.
            if (File.Exists(Path.Combine(programDirectory, HistoryDatabaseFileName)) && !File.Exists(HistoryDatabase))
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                    CopyFileIfMissing(programDirectory, HistoryDatabaseFileName + suffix);

            foreach (var folder in new[] { LogsFolderName, IconCacheFolderName, PacketLogsFolderName })
                CopyFolderFilesIfMissing(Path.Combine(programDirectory, folder), Path.Combine(DataDirectory, folder));
        }

        private static void CopyFileIfMissing(string programDirectory, string fileName)
        {
            var source = Path.Combine(programDirectory, fileName);
            var target = Path.Combine(DataDirectory, fileName);
            if (File.Exists(source) && !File.Exists(target))
                File.Copy(source, target);
        }

        private static void CopyFolderFilesIfMissing(string sourceFolder, string targetFolder)
        {
            if (!Directory.Exists(sourceFolder)) return;

            foreach (var source in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(targetFolder, Path.GetRelativePath(sourceFolder, source));
                if (File.Exists(target)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target);
            }
        }
    }
}
