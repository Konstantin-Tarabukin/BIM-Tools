using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.IO.Compression;


namespace BIMToolsUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("BIM Tools Updater запущен.");

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
                        Console.ReadKey();

                        Console.WriteLine(
    $"Файлов в распакованной папке: " +
    $"{Directory.GetFiles(testDirectory, "*", SearchOption.AllDirectories).Length}");

                        string targetDirectory =
                            Path.Combine(
                                Environment.GetFolderPath(
                                    Environment.SpecialFolder.ApplicationData),
                                "Autodesk",
                                "Revit",
                                "Addins",
                                "2023",
                                "RevitCopyParams");

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

                            File.Copy(
                                sourceFile,
                                targetFile,
                                true);

                            Console.WriteLine(
                                $"Файл обновлён: {targetFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Ошибка распаковки обновления:");

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

                Console.WriteLine("Updater завершает работу.");

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
    }
}