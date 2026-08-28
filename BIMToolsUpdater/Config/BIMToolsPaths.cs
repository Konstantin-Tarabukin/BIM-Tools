using System;
using System.IO;

namespace BIMToolsUpdater.Config
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

        public static string UpdaterLogPath
        {
            get
            {
                return Path.Combine(
                    LogsDirectory,
                    "Updater.log");
            }
        }

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(
                BIMToolsDirectory);

            Directory.CreateDirectory(
                LogsDirectory);
        }
    }
}