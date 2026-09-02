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

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(
                BIMToolsDirectory);

            Directory.CreateDirectory(
                LogsDirectory);

            Directory.CreateDirectory(
                TempDirectory);
        }
    }
}