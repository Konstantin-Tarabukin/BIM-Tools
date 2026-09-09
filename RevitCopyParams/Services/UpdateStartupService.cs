using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    public static class UpdateStartupService
    {
        private static bool updateStarted;

        public static void LogDiagnostic(string message)
        {
            try
            {
                BIMToolsPaths.EnsureDirectories();

                File.AppendAllText(
                    BIMToolsPaths.JournalDiagnosticsLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " | " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void Start()
        {
            if (updateStarted)
                return;

            updateStarted = true;

            Task.Run(async () =>
            {
                await CheckForUpdates();
            });
        }

        private static async Task CheckForUpdates()
        {
            BIMToolsPaths.EnsureDirectories();

            string logPath =
                BIMToolsPaths.UpdateStartupLogPath;

            try
            {
                File.WriteAllText(
                    logPath,
                    "1. Автоматическая проверка обновления запущена\n");

                UpdateInfo updateInfo =
                    await UpdateService.GetLatestRelease();

                File.AppendAllText(
                    logPath,
                    "2. GetLatestRelease завершён\n" +
                    "CurrentVersion: " +
                    updateInfo.CurrentVersion + "\n" +
                    "LatestVersion: " +
                    updateInfo.LatestVersion + "\n" +
                    "UpdateAvailable: " +
                    updateInfo.UpdateAvailable + "\n" +
                    "DownloadUrl: " +
                    updateInfo.DownloadUrl + "\n");

                if (!updateInfo.UpdateAvailable)
                {
                    File.AppendAllText(
                        logPath,
                        "3. Обновление не требуется\n");

                    return;
                }

                File.AppendAllText(
                    logPath,
                    "3. Начинаем DownloadUpdate\n");

                string downloadedFile =
                    await UpdateService.DownloadUpdate(
                        updateInfo);

                File.AppendAllText(
                    logPath,
                    "4. DownloadUpdate завершён\n" +
                    "DownloadedZip: " +
                    downloadedFile + "\n");

                if (!File.Exists(downloadedFile))
                {
                    File.AppendAllText(
                        logPath,
                        "5. ОШИБКА: ZIP не существует\n");

                    return;
                }

                FileInfo fileInfo =
                    new FileInfo(downloadedFile);

                File.AppendAllText(
                    logPath,
                    "5. ZIP существует\n" +
                    "Размер: " +
                    fileInfo.Length +
                    " bytes\n");

                File.AppendAllText(
                    logPath,
                    "6. Проверяем UpdateInfo.json\n");

                Version packageVersion =
                    UpdateService.GetPackageVersion(
                        downloadedFile);

                File.AppendAllText(
                    logPath,
                    "PackageVersion: " +
                    packageVersion +
                    "\n" +
                    "CurrentVersion: " +
                    updateInfo.CurrentVersion +
                    "\n");

                UpdateService.ValidateUpdatePackage(
                    downloadedFile,
                    updateInfo.CurrentVersion);

                File.AppendAllText(
                    logPath,
                    "PackageVersionValid: True\n");

                File.AppendAllText(
                    logPath,
                    "7. Запускаем Updater\n");

                bool updaterStarted =
                    StartUpdater(
                        downloadedFile,
                        logPath);

                if (updaterStarted)
                {
                    File.AppendAllText(
                        logPath,
                        "8. Updater успешно запущен\n");
                }
                else
                {
                    File.AppendAllText(
                        logPath,
                        "8. ОШИБКА: Updater не был запущен\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    logPath,
                    "\n!!! ОШИБКА !!!\n" +
                    ex.ToString() +
                    "\n");
            }
        }

        private static bool StartUpdater(
            string updatePath,
            string logPath)
        {
            try
            {
                string assemblyPath =
                    System.Reflection.Assembly
                        .GetExecutingAssembly()
                        .Location;

                string assemblyFolder =
                    Path.GetDirectoryName(
                        assemblyPath);

                string updaterPath =
                    Path.Combine(
                        assemblyFolder,
                        "Updater",
                        "BIMToolsUpdater.exe");

                if (!File.Exists(updaterPath))
                {
                    File.AppendAllText(
                        logPath,
                        "Updater не найден:\n" +
                        updaterPath +
                        "\n");

                    return false;
                }

                int revitProcessId =
                    System.Diagnostics.Process
                        .GetCurrentProcess()
                        .Id;

                string arguments =
                    $"{revitProcessId} \"{updatePath}\"";

                System.Diagnostics.Process.Start(
                    updaterPath,
                    arguments);

                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText(
                    logPath,
                    "Ошибка запуска Updater:\n" +
                    ex +
                    "\n");

                return false;
            }
        }
    }
}