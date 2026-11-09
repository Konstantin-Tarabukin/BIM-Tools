using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace RevitCopyParams.Services
{
    // Определение корня внешнего журнала проекта (ADR-014, этап A.4).
    //
    // Порядок определения корня:
    //   1. явное переопределение JournalSettings.JournalRootOverride;
    //   2. автопоиск вверх по дереву каталогов от central-файла
    //      уже существующей папки _BIMToolsJournal (до 3 уровней вверх);
    //   3. по умолчанию — _BIMToolsJournal рядом с central-файлом.
    //
    // Метод обращается к Revit API и файловой системе,
    // поэтому вызывается только из Revit API-потока.
    // Результат кэшируется, чтобы polling на background-потоке
    // не обращался ни к Revit API, ни к сети при каждом тике.
    public static class JournalPathResolver
    {
        public const string JournalFolderName = "_BIMToolsJournal";

        private const int MaxUpwardLevels = 3;

        // При недоступности сети Directory.Exists может выполняться
        // секунды. Неудачные попытки кэшируются на 5 минут, чтобы
        // каждое событие Revit (открытие, синхронизация) не повторяло
        // медленный поиск (A.15: не блокировать пользовательские операции).
        private const int FailedResolveCacheSeconds = 300;

        private static readonly object CacheLock = new object();

        private static readonly Dictionary<string, JournalLocation> Cache =
            new Dictionary<string, JournalLocation>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, DateTime> FailedCache =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        public static JournalLocation Resolve(Document document)
        {
            try
            {
                if (document == null)
                    return null;

                if (!document.IsWorkshared)
                {
                    Log("JOURNAL RESOLVE SKIP | Reason=NotWorkshared");

                    return null;
                }

                ModelPath centralPath =
                    document.GetWorksharingCentralModelPath();

                if (centralPath == null)
                {
                    Log("JOURNAL RESOLVE SKIP | Reason=NoCentralPath");

                    return null;
                }

                string centralFile =
                    ModelPathUtils.ConvertModelPathToUserVisiblePath(
                        centralPath);

                if (string.IsNullOrWhiteSpace(centralFile))
                {
                    Log("JOURNAL RESOLVE SKIP | Reason=EmptyCentralPath");

                    return null;
                }

                string centralDirectory =
                    Path.GetDirectoryName(centralFile);

                if (string.IsNullOrEmpty(centralDirectory))
                {
                    Log("JOURNAL RESOLVE SKIP | Reason=NoCentralDirectory");

                    return null;
                }

                lock (CacheLock)
                {
                    JournalLocation cached;

                    if (Cache.TryGetValue(centralDirectory, out cached))
                        return cached;

                    DateTime failedAt;

                    if (FailedCache.TryGetValue(centralDirectory, out failedAt) &&
                        (DateTime.UtcNow - failedAt).TotalSeconds < FailedResolveCacheSeconds)
                    {
                        // Недавно поиск уже закончился неудачей — не повторяем.
                        return null;
                    }

                    JournalLocation location =
                        ResolveInternal(centralFile, centralDirectory);

                    if (location != null)
                    {
                        Cache[centralDirectory] = location;
                    }
                    else
                    {
                        FailedCache[centralDirectory] = DateTime.UtcNow;
                    }

                    return location;
                }
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL RESOLVE ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);

                return null;
            }
        }


        private static JournalLocation ResolveInternal(
            string centralFile,
            string centralDirectory)
        {
            string modelName =
                Path.GetFileNameWithoutExtension(centralFile);

            string journalRoot =
                ResolveRoot(centralDirectory);

            string projectDirectory =
                Path.GetDirectoryName(journalRoot);

            string projectId =
                projectDirectory != null
                    ? new DirectoryInfo(projectDirectory).Name
                    : "UNKNOWN";

            string modelId;

            if (string.Equals(
                    centralDirectory,
                    projectDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Central-файл лежит прямо в корне проекта.
                modelId = modelName;
            }
            else
            {
                modelId = new DirectoryInfo(centralDirectory).Name;
            }

            JournalLocation location = new JournalLocation
            {
                JournalRoot = journalRoot,
                ProjectId = projectId,
                ModelId = modelId,
                ModelName = modelName
            };

            Log(
                "JOURNAL RESOLVED | Project=" +
                projectId +
                " | ModelId=" +
                modelId +
                " | ModelName=" +
                modelName +
                " | Root=" +
                journalRoot);

            return location;
        }


        private static string ResolveRoot(string centralDirectory)
        {
            // 1. Явное переопределение из настроек (A.4).
            string overrideRoot =
                JournalSettings.JournalRootOverride;

            if (!string.IsNullOrWhiteSpace(overrideRoot))
                return overrideRoot;

            // 2. Автопоиск существующего корня вверх по дереву.
            string found =
                FindExistingRoot(centralDirectory);

            if (found != null)
                return found;

            // 3. По умолчанию — рядом с central-файлом.
            return Path.Combine(centralDirectory, JournalFolderName);
        }


        private static string FindExistingRoot(string startDirectory)
        {
            string current = startDirectory;

            for (int level = 0;
                 level <= MaxUpwardLevels &&
                 !string.IsNullOrEmpty(current);
                 level++)
            {
                string candidate = Path.Combine(
                    current,
                    JournalFolderName);

                try
                {
                    if (Directory.Exists(candidate))
                        return candidate;
                }
                catch (Exception ex)
                {
                    Log(
                        "JOURNAL RESOLVE EXISTS ERROR | Path=" +
                        candidate +
                        " | Message=" +
                        ex.Message);
                }

                current = Path.GetDirectoryName(current);
            }

            return null;
        }

        private static void Log(string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(message);
            }
            catch
            {
                // Ошибка логирования не должна влиять на Revit.
            }
        }
    }
}
