using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.IO.Compression;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace BIMToolsUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
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

                    return;
                }

                RunUpdater(args);
            }
        }

        static void RunUpdater(string[] args)
        {
            Console.WriteLine(
                "BIM Tools Updater запущен.");



     if (args.Length == 0)
     {
         Console.WriteLine("PID Revit не передан.");
         Console.ReadKey();
         return;
     }

     int revitProcessId;

     if (!int.TryParse(args[0], out revitProcessId))
     {
         Console.WriteLine("Некорректный PID Revit.");
         Console.ReadKey();
         return;
     }

     string updatePath = null;

     if (args.Length >= 2)
     {
         updatePath = args[1];

         Console.WriteLine(
             $"Получен путь обновления: {updatePath}");
     }
     else
     {
         Console.WriteLine(
             "Путь обновления не передан.");
     }

     Console.WriteLine(
         $"Ожидание завершения Revit. PID: {revitProcessId}");

     try
     {
         Process revitProcess =
             Process.GetProcessById(revitProcessId);

         revitProcess.WaitForExit();

         Console.WriteLine("Revit закрыт.");

         if (!string.IsNullOrEmpty(updatePath))
         {
             if (!File.Exists(updatePath))
             {
                 Console.WriteLine(
                     $"Файл обновления не найден: {updatePath}");

                 Console.ReadKey();
                 return;
             }

             string testDirectory =
                 @"C:\Temp\BIMToolsUpdate_Test";

             try
             {
                 if (Directory.Exists(testDirectory))
                 {
                     Directory.Delete(testDirectory, true);
                 }

                 Directory.CreateDirectory(testDirectory);

                 ZipFile.ExtractToDirectory(
                     updatePath,
                     testDirectory);

                 Console.WriteLine(
                     $"Обновление распаковано в: {testDirectory}");

                 Console.WriteLine("=== НОВАЯ ВЕРСИЯ ТЕСТА ===");

                 int extractedFileCount =
                     Directory.GetFiles(
                         testDirectory,
                         "*",
                         SearchOption.AllDirectories).Length;

                 Console.WriteLine(
                     "Файлов в распакованной папке: " +
                     extractedFileCount);

                 string targetDirectory =
                     Path.Combine(
                         Environment.GetFolderPath(
                             Environment.SpecialFolder.ApplicationData),
                         "Autodesk",
                         "Revit",
                         "Addins",
                         "2023",
                         "RevitCopyParams");

                 List<string> backupFiles =
                     new List<string>();

                 List<string> newFiles =
                     new List<string>();

                 try
                 {
                     foreach (string sourceFile in Directory.GetFiles(
                         testDirectory,
                         "*",
                         SearchOption.AllDirectories))
                     {
                         string relativePath =
                             sourceFile.Substring(
                                 testDirectory.Length)
                             .TrimStart('\\');

                         string targetFile =
                             Path.Combine(
                                 targetDirectory,
                                 relativePath);

                         string targetFolder =
                             Path.GetDirectoryName(targetFile);

                         if (!Directory.Exists(targetFolder))
                         {
                             Directory.CreateDirectory(targetFolder);
                         }

                         string backupFile =
                             targetFile + ".backup";

                         if (File.Exists(targetFile))
                         {
                             Console.WriteLine(
                                 $"Создание backup: {backupFile}");

                             File.Copy(
                                 targetFile,
                                 backupFile,
                                 true);

                             backupFiles.Add(backupFile);
                         }
                         else
                         {
                             Console.WriteLine(
                                 $"Новый файл: {targetFile}");

                             newFiles.Add(targetFile);
                         }

                         Console.WriteLine(
                             $"Замена файла: {targetFile}");

                         File.Copy(
                             sourceFile,
                             targetFile,
                             true);

                         Console.WriteLine(
                             $"Файл обновлён: {targetFile}");


                     }

                     Console.WriteLine(
                         "Все файлы успешно обновлены.");

                     foreach (string backupFile in backupFiles)
                     {
                         if (File.Exists(backupFile))
                         {
                             File.Delete(backupFile);
                         }
                     }

                            Console.WriteLine(
                                "Backup-файлы удалены.");

                            Console.WriteLine(
                                "=== ОБНОВЛЕНИЕ УСПЕШНО ЗАВЕРШЕНО ===");

                            ShowUpdateCompletedMessage();
                        }
                        catch (Exception updateException)
                        {
                            Console.WriteLine(
                                "!!! ОШИБКА ОБНОВЛЕНИЯ !!!");

                            Console.WriteLine(
                                updateException.ToString());

                            Console.WriteLine(
                                "Запускается восстановление предыдущей версии...");

                            bool rollbackSuccess = true;

                     foreach (string backupFile in backupFiles)
                     {
                         try
                         {
                             if (!File.Exists(backupFile))
                             {
                                 Console.WriteLine(
                                     $"Backup не найден: {backupFile}");

                                 rollbackSuccess = false;
                                 continue;
                             }

                             string originalFile =
                                 backupFile.Substring(
                                     0,
                                     backupFile.Length -
                                     ".backup".Length);

                             File.Copy(
                                 backupFile,
                                 originalFile,
                                 true);

                             File.Delete(backupFile);

                             Console.WriteLine(
                                 $"Восстановлен файл: {originalFile}");
                         }
                         catch (Exception rollbackException)
                         {
                             rollbackSuccess = false;

                             Console.WriteLine(
                                 $"Ошибка восстановления {backupFile}:");

                             Console.WriteLine(
                                 rollbackException.ToString());
                         }
                     }

                     foreach (string newFile in newFiles)
                     {
                         try
                         {
                             if (File.Exists(newFile))
                             {
                                 File.Delete(newFile);

                                 Console.WriteLine(
                                     $"Удалён новый файл: {newFile}");
                             }
                         }
                         catch (Exception deleteException)
                         {
                             rollbackSuccess = false;

                             Console.WriteLine(
                                 $"Ошибка удаления нового файла {newFile}:");

                             Console.WriteLine(
                                 deleteException.ToString());
                         }
                     }

                     if (rollbackSuccess)
                     {
                         Console.WriteLine(
                             "=== ROLLBACK УСПЕШНО ЗАВЕРШЁН ===");

                         Console.WriteLine(
                             "Предыдущая версия восстановлена.");
                     }
                     else
                     {
                         Console.WriteLine(
                             "=== ROLLBACK ЗАВЕРШЁН С ОШИБКАМИ ===");

                         Console.WriteLine(
                             "Необходимо проверить файлы плагина вручную.");
                     }

                     Console.ReadKey();
                     return;
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine(
                     "Ошибка подготовки обновления:");

                 Console.WriteLine(ex.ToString());

                 Console.ReadKey();
                 return;
             }
         }

         if (!string.IsNullOrEmpty(updatePath))
         {
             Console.WriteLine(
                 $"Путь обновления после закрытия Revit: {updatePath}");
         }

         Console.WriteLine(
             "Updater завершает работу.");

         Thread.Sleep(2000);
     }
     catch (ArgumentException)
     {
         Console.WriteLine(
             "Процесс Revit уже завершён.");
     }
     catch (Exception ex)
     {
         Console.WriteLine(ex.ToString());
         Console.ReadKey();
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
                        SystemParameters.WorkArea.Right - window.Width - 20;

                    window.Top =
                        SystemParameters.WorkArea.Bottom - window.Height - 20;

                    window.AllowsTransparency =
                        true;

                    window.Background =
                        Brushes.Transparent;

                    Border border =
                        new Border();

                    border.Background =
                        new SolidColorBrush(
                            Color.FromRgb(45, 45, 48));

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