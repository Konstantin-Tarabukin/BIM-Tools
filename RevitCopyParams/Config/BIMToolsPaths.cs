using System;
using System.IO;

namespace RevitCopyParams.Config
{
    public static class BIMToolsPaths
    {
        public static string AppDataDirectory
        {
            get
            {
                return Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);
            }
        }

        public static string BIMToolsDirectory
        {
            get
            {
                return Path.Combine(
                    AppDataDirectory,
                    "BIMTools",
                    "RevitCopyParams");
            }
        }

        public static string LogsDirectory
        {
            get
            {
                return Path.Combine(
                    BIMToolsDirectory,
                    "Logs");
            }
        }

        public static string UpdatesDirectory
        {
            get
            {
                return Path.Combine(
                    BIMToolsDirectory,
                    "Updates");
            }
        }

        public static string TempDirectory
        {
            get
            {
                return Path.Combine(
                    BIMToolsDirectory,
                    "Temp");
            }
        }


        public static string PluginDirectory
        {
            get
            {
                return Path.Combine(
                    AppDataDirectory,
                    "Autodesk",
                    "Revit",
                    "Addins",
                    "2023",
                    "RevitCopyParams");
            }
        }

        public static string UpdaterPath
        {
            get
            {
                return Path.Combine(
                    PluginDirectory,
                    "Updater",
                    "BIMToolsUpdater.exe");
            }
        }

        public static string UpdateStartupLogPath
        {
            get
            {
                return Path.Combine(
                    LogsDirectory,
                    "UpdateStartup.log");
            }
        }

        public static string RevitLinkReloadLogPath
        {
            get
            {
                return Path.Combine(
                    LogsDirectory,
                    "RevitLinkReload.log");
            }
        }

        public static string UpdaterLogPath
        {
            get
            {
                return Path.Combine(
                    LogsDirectory,
                    "Updater.log");
            }
        }

        public static string JournalDiagnosticsLogPath
        {
            get
            {
                return Path.Combine(
                    LogsDirectory,
                    "JournalDiagnostics.log");
            }
        }

        // Локальное состояние обработки внешних JSON-событий
        // (ADR-014, этап A.11). Хранится только у пользователя.
        public static string JournalStateJsonPath
        {
            get
            {
                return Path.Combine(
                    BIMToolsDirectory,
                    "JournalState.json");
            }
        }

        // Файл настроек внешнего JSON-журнала.
        public static string JournalSettingsJsonPath
        {
            get
            {
                return Path.Combine(
                    BIMToolsDirectory,
                    "JournalSettings.json");
            }
        }

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(
                BIMToolsDirectory);

            Directory.CreateDirectory(
                LogsDirectory);

            Directory.CreateDirectory(
                UpdatesDirectory);

            Directory.CreateDirectory(
                TempDirectory);


        }
    }
}