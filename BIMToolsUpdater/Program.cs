
using BIMToolsUpdater.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BIMToolsUpdater
{
    internal class Program
    {
        private static readonly string LogPath =
            BIMToolsPaths.UpdaterLogPath;

        static void Main(string[] args)
        {
            BIMToolsPaths.EnsureDirectories();

            bool createdNew;

            using (Mutex updaterMutex =
                new Mutex(
                    true,
                    "Global\\BIMToolsUpdater",
                    out createdNew))
            {
                if (!createdNew)
                {
                    Console.WriteLine(
                        "BIM Tools Updater уже запущен.");

                    Log(
                        "Updater уже запущен. Второй экземпляр завершён.");

                    return;
                }

                Log(
                    "=== BIM TOOLS UPDATER START ===");

                RunUpdater(args);
            }
        }

        static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    " | " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
                // Логирование не должно ломать работу Updater.
            }
        }

        static void RunUpdater(string[] args)
        {
            Console.WriteLine(
                "BIM Tools Updater запущен.");

            Log(
                "BIM Tools Updater запущен.");

            if (args.Length == 0)
            {
                Console.WriteLine(
                    "PID Revit не передан.");

                Log(
                    "ОШИБКА: PID Revit не передан.");

                Thread.Sleep(2000);
                return;
            }

            int revitProcessId;

            if (!int.TryParse(
                args[0],
                out revitProcessId))
            {
                Console.WriteLine(
                    "Некорректный PID Revit.");

                Log(
                    "ОШИБКА: некорректный PID Revit.");

                Thread.Sleep(2000);
                return;
            }

            string updatePath = null;

            if (args.Length >= 2)
            {
                updatePath = args[1];

                Console.WriteLine(
                    $"Получен путь обновления: {updatePath}");

                Log(
                    $"Получен путь обновления: {updatePath}");
            }
            else
            {
                Console.WriteLine(
                    "Путь обновления не передан.");

                Log(
                    "ОШИБКА: путь обновления не передан.");
            }

            Console.WriteLine(
                $"Ожидание завершения Revit. PID: {revitProcessId}");

            Log(
                $"Ожидание завершения Revit. PID: {revitProcessId}");

            try
            {
                Process revitProcess =
                    Process.GetProcessById(
                        revitProcessId);

                revitProcess.WaitForExit();

                Console.WriteLine(
                    "Revit закрыт.");

                Log(
                    "Revit закрыт.");

                if (!string.IsNullOrEmpty(updatePath))
                {
                    if (!File.Exists(updatePath))
                    {
                        Console.WriteLine(
                            $"Файл обновления не найден: {updatePath}");

                        Log(
                            $"ОШИБКА: файл обновления не найден: {updatePath}");

                        Thread.Sleep(2000);
                        return;
                    }

                    Log(
                        "ZIP-файл существует.");

                    string tempDirectory =
                        BIMToolsPaths.TempDirectory;

                    try
                    {
                        if (Directory.Exists(tempDirectory))
                        {
                            Directory.Delete(
                                tempDirectory,
                                true);

                            Log(
                                $"Удалена старая временная папка: {tempDirectory}");
                        }

                        Directory.CreateDirectory(
                            tempDirectory);

                        Log(
                            $"Создана временная папка: {tempDirectory}");

                        ZipFile.ExtractToDirectory(
                            updatePath,
                            tempDirectory);

                        Console.WriteLine(
                            $"Обновление распаковано в: {tempDirectory}");

                        Log(
                            $"ZIP распакован в: {tempDirectory}");

                        int extractedFileCount =
                            Directory.GetFiles(
                                tempDirectory,
                                "*",
                                SearchOption.AllDirectories).Length;

                        Console.WriteLine(
                            "Файлов в распакованной папке: " +
                            extractedFileCount);

                        Log(
                            $"Файлов в распакованном пакете: {extractedFileCount}");

                        string targetDirectory =
                            BIMToolsPaths.PluginDirectory;

                        Log(
                            $"Целевая папка: {targetDirectory}");

                        List<string> backupFiles =
                            new List<string>();

                        List<string> newFiles =
                            new List<string>();

                        try
                        {

                            foreach (string sourceFile in
                                Directory.GetFiles(
                                    tempDirectory,
                                    "*",
                                    SearchOption.AllDirectories))
                            {
                                string relativePath =
                                    sourceFile.Substring(
                                        tempDirectory.Length)
                                    .TrimStart('\\');

                                string targetFile =
                                    Path.Combine(
                                        targetDirectory,
                                        relativePath);

                                string targetFolder =
                                    Path.GetDirectoryName(
                                        targetFile);

                                if (!Directory.Exists(
                                    targetFolder))
                                {
                                    Directory.CreateDirectory(
                                        targetFolder);

                                    Log(
                                        $"Создана папка: {targetFolder}");
                                }

                                string backupFile =
                                    targetFile + ".backup";

                                if (File.Exists(
                                    targetFile))
                                {
                                    Console.WriteLine(
                                        $"Создание backup: {backupFile}");

                                    Log(
                                        $"Создание backup: {backupFile}");

                                    File.Copy(
                                        targetFile,
                                        backupFile,
                                        true);

                                    backupFiles.Add(
                                        backupFile);

                                    Log(
                                        $"Backup добавлен в список: {backupFile}");
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"Новый файл: {targetFile}");

                                    Log(
                                        $"Новый файл: {targetFile}");

                                    newFiles.Add(
                                        targetFile);
                                }

                                Console.WriteLine(
                                    $"Замена файла: {targetFile}");

                                Log(
                                    $"Замена файла: {targetFile}");

                                File.Copy(
                                    sourceFile,
                                    targetFile,
                                    true);

                                Console.WriteLine(
                                    $"Файл обновлён: {targetFile}");

                                Log(
                                    $"Файл успешно заменён: {targetFile}");


                                }

                            

                            Console.WriteLine(
                                "Все файлы успешно обновлены.");

                            Log(
                                "Все файлы успешно обновлены.");

                            foreach (string backupFile in backupFiles)
                            {
                                if (File.Exists(
                                    backupFile))
                                {
                                    File.Delete(
                                        backupFile);

                                    Log(
                                        $"Backup удалён: {backupFile}");
                                }
                            }

                            try
                            {
                                if (File.Exists(
                                    updatePath))
                                {
                                    File.Delete(
                                        updatePath);

                                    Console.WriteLine(
                                        $"Пакет обновления удалён: {updatePath}");

                                    Log(
                                        $"Пакет обновления удалён: {updatePath}");
                                }
                            }
                            catch (Exception cleanupException)
                            {
                                Console.WriteLine(
                                    "Не удалось удалить пакет обновления:");

                                Console.WriteLine(
                                    cleanupException.ToString());

                                Log(
                                    "ОШИБКА удаления пакета обновления: " +
                                    cleanupException);
                            }

                            Console.WriteLine(
                                "Backup-файлы удалены.");

                            Log(
                                "Backup-файлы удалены.");

                            Console.WriteLine(
                                "=== ОБНОВЛЕНИЕ УСПЕШНО ЗАВЕРШЕНО ===");

                            Log(
                                "=== ОБНОВЛЕНИЕ УСПЕШНО ЗАВЕРШЕНО ===");

                            ShowUpdateCompletedMessage();
                        }
                        catch (Exception updateException)
                        {
                            Console.WriteLine(
                                "!!! ОШИБКА ОБНОВЛЕНИЯ !!!");

                            Console.WriteLine(
                                updateException.ToString());

                            Log(
                                "!!! ОШИБКА ОБНОВЛЕНИЯ !!!");

                            Log(
                                updateException.ToString());

                            Console.WriteLine(
                                "Запускается восстановление предыдущей версии...");

                            Log(
                                "Запускается восстановление предыдущей версии.");

                            bool rollbackSuccess =
                                true;

                            Log(
                                $"Backup-файлов для восстановления: {backupFiles.Count}");

                            Log(
                                $"Новых файлов для удаления: {newFiles.Count}");

                            foreach (string backupFile in backupFiles)
                            {
                                try
                                {
                                    if (!File.Exists(
                                        backupFile))
                                    {
                                        Console.WriteLine(
                                            $"Backup не найден: {backupFile}");

                                        Log(
                                            $"ОШИБКА: Backup не найден: {backupFile}");

                                        rollbackSuccess =
                                            false;

                                        continue;
                                    }

                                    string originalFile =
                                        backupFile.Substring(
                                            0,
                                            backupFile.Length -
                                            ".backup".Length);

                                    Log(
                                        $"Восстановление: {originalFile}");

                                    File.Copy(
                                        backupFile,
                                        originalFile,
                                        true);

                                    File.Delete(
                                        backupFile);

                                    Console.WriteLine(
                                        $"Восстановлен файл: {originalFile}");

                                    Log(
                                        $"Файл восстановлен: {originalFile}");

                                    Log(
                                        $"Backup удалён после восстановления: {backupFile}");
                                }
                                catch (Exception rollbackException)
                                {
                                    rollbackSuccess =
                                        false;

                                    Console.WriteLine(
                                        $"Ошибка восстановления {backupFile}:");

                                    Console.WriteLine(
                                        rollbackException.ToString());

                                    Log(
                                        $"ОШИБКА восстановления {backupFile}: " +
                                        rollbackException);
                                }
                            }

                            foreach (string newFile in newFiles)
                            {
                                try
                                {
                                    if (File.Exists(
                                        newFile))
                                    {
                                        File.Delete(
                                            newFile);

                                        Console.WriteLine(
                                            $"Удалён новый файл: {newFile}");

                                        Log(
                                            $"Новый файл удалён: {newFile}");
                                    }
                                }
                                catch (Exception deleteException)
                                {
                                    rollbackSuccess =
                                        false;

                                    Console.WriteLine(
                                        $"Ошибка удаления нового файла {newFile}:");

                                    Console.WriteLine(
                                        deleteException.ToString());

                                    Log(
                                        $"ОШИБКА удаления нового файла {newFile}: " +
                                        deleteException);
                                }
                            }

                            if (rollbackSuccess)
                            {
                                Console.WriteLine(
                                    "=== ROLLBACK УСПЕШНО ЗАВЕРШЁН ===");

                                Console.WriteLine(
                                    "Предыдущая версия восстановлена.");

                                Log(
                                    "=== ROLLBACK УСПЕШНО ЗАВЕРШЁН ===");

                                Log(
                                    "Предыдущая версия восстановлена.");
                            }
                            else
                            {
                                Console.WriteLine(
                                    "=== ROLLBACK ЗАВЕРШЁН С ОШИБКАМИ ===");

                                Console.WriteLine(
                                    "Необходимо проверить файлы плагина вручную.");

                                Log(
                                    "=== ROLLBACK ЗАВЕРШЁН С ОШИБКАМИ ===");

                                Log(
                                    "Необходимо проверить файлы плагина вручную.");
                            }

                            Log(
                                "ZIP после ошибки обновления НЕ удаляется.");

                            Thread.Sleep(2000);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Ошибка подготовки обновления:");

                        Console.WriteLine(
                            ex.ToString());

                        Log(
                            "Ошибка подготовки обновления: " +
                            ex);

                        Thread.Sleep(2000);
                        return;
                    }
                }



                Console.WriteLine(
                    "Updater завершает работу.");

                Log(
                    "Updater завершает работу.");

                Thread.Sleep(2000);
            }
            catch (ArgumentException)
            {
                Console.WriteLine(
                    "Процесс Revit уже завершён.");

                Log(
                    "Процесс Revit уже завершён.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    ex.ToString());

                Log(
                    "Критическая ошибка Updater: " +
                    ex);

                Thread.Sleep(2000);
            }
        }

        static void ShowUpdateCompletedMessage()
        {
            Thread notificationThread =
                new Thread(() =>
                {
                    Application application =
                        new Application();

                    Window window =
                        new Window();

                    window.WindowStyle =
                        WindowStyle.None;

                    window.ResizeMode =
                        ResizeMode.NoResize;

                    window.ShowInTaskbar =
                        false;

                    window.Topmost =
                        true;

                    window.Width =
                        360;

                    window.Height =
                        100;

                    window.Left =
                        SystemParameters.WorkArea.Right -
                        window.Width -
                        20;

                    window.Top =
                        SystemParameters.WorkArea.Bottom -
                        window.Height -
                        20;

                    window.AllowsTransparency =
                        true;

                    window.Background =
                        Brushes.Transparent;

                    Border border =
                        new Border();

                    border.Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                45,
                                45,
                                48));

                    border.CornerRadius =
                        new CornerRadius(8);

                    border.Padding =
                        new Thickness(20);

                    TextBlock text =
                        new TextBlock();

                    text.Text =
                        "BIM Tools\n\n" +
                        "Обновление успешно установлено.";

                    text.Foreground =
                        Brushes.White;

                    text.FontSize =
                        14;

                    text.VerticalAlignment =
                        VerticalAlignment.Center;

                    border.Child =
                        text;

                    window.Content =
                        border;

                    window.Show();

                    Thread.Sleep(3000);

                    window.Dispatcher.Invoke(
                        new Action(() =>
                        {
                            window.Close();
                        }));

                    application.Shutdown();
                });

            notificationThread.SetApartmentState(
                ApartmentState.STA);

            notificationThread.IsBackground =
                true;

            notificationThread.Start();
        }
    }
}

