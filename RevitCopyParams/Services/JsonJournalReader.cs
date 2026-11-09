using Newtonsoft.Json;
using RevitCopyParams.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitCopyParams.Services
{
    public class JsonJournalReader : IJournalReader
    {
        public List<JournalEventFile> ReadSince(
            string journalRoot,
            DateTime sinceUtc,
            IEnumerable<string> pendingRelativePaths)
        {
            List<JournalEventFile> result =
                new List<JournalEventFile>();

            if (string.IsNullOrWhiteSpace(journalRoot))
                return result;

            if (!Directory.Exists(journalRoot))
                return result;

            DateTime nowUtc = DateTime.UtcNow;
            DateTime startUtc =
                sinceUtc
                    .ToUniversalTime()
                    .Subtract(
                        TimeSpan.FromSeconds(60));

            if (startUtc > nowUtc)
                startUtc = nowUtc;

            HashSet<string> pathsAlreadyRead =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DateTime day = startUtc.Date;

            while (day.Date <= nowUtc.Date)
            {
                string dayDirectory = Path.Combine(
                    journalRoot,
                    day.ToString("yyyy"),
                    day.ToString("MM"),
                    day.ToString("dd"));

                if (Directory.Exists(dayDirectory))
                {
                    string[] modelDirectories =
                        Directory.GetDirectories(dayDirectory);

                    foreach (string modelDirectory in modelDirectories)
                    {
                        string[] files;

                        try
                        {
                            files = Directory.GetFiles(
                                modelDirectory,
                                "*.json",
                                SearchOption.TopDirectoryOnly);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (string filePath in files)
                        {
                            DateTime lastWriteUtc;

                            try
                            {
                                lastWriteUtc =
                                    File.GetLastWriteTimeUtc(filePath);
                            }
                            catch
                            {
                                continue;
                            }

                            if (lastWriteUtc < startUtc)
                                continue;

                            string relativePath =
                                GetRelativePath(
                                    journalRoot,
                                    filePath);

                            if (!pathsAlreadyRead.Add(relativePath))
                                continue;

                            JournalEventFile parsed =
                                TryReadEvent(
                                    filePath,
                                    relativePath);

                            if (parsed != null)
                                result.Add(parsed);
                        }
                    }
                }

                day = day.AddDays(1);
            }

            if (pendingRelativePaths != null)
            {
                foreach (string relativePath in pendingRelativePaths)
                {
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    if (!pathsAlreadyRead.Add(relativePath))
                        continue;

                    string fullPath =
                        Path.Combine(
                            journalRoot,
                            relativePath);

                    if (!IsPathInsideRoot(
                            journalRoot,
                            fullPath))
                    {
                        continue;
                    }

                    if (!File.Exists(fullPath))
                        continue;

                    JournalEventFile parsed =
                        TryReadEvent(
                            fullPath,
                            relativePath);

                    if (parsed != null)
                        result.Add(parsed);
                }
            }

            return result
                .OrderBy(x => x.Event.CreatedUtc)
                .ThenBy(x => x.Event.EventId)
                .ToList();
        }

        private static JournalEventFile TryReadEvent(
            string filePath,
            string relativePath)
        {
            try
            {
                string json =
                    File.ReadAllText(filePath);

                JournalEvent journalEvent =
                    JsonConvert.DeserializeObject<JournalEvent>(json);

                if (journalEvent == null)
                    return null;

                if (journalEvent.SchemaVersion != 1)
                    return null;

                if (journalEvent.EventId == Guid.Empty)
                    return null;

                if (string.IsNullOrWhiteSpace(journalEvent.ProjectId))
                    return null;

                if (string.IsNullOrWhiteSpace(journalEvent.ModelId))
                    return null;

                if (journalEvent.TargetModels == null)
                    journalEvent.TargetModels =
                        new List<string>();

                if (journalEvent.Elements == null)
                    journalEvent.Elements =
                        new List<ChangeElementReference>();

                return new JournalEventFile(
                    journalEvent,
                    relativePath);
            }
            catch
            {
                return null;
            }
        }

        private static string GetRelativePath(
            string root,
            string fullPath)
        {
            string normalizedRoot =
                Path.GetFullPath(root)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            string normalizedFile =
                Path.GetFullPath(fullPath);

            if (!normalizedFile.StartsWith(
                    normalizedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileName(fullPath);
            }

            return normalizedFile.Substring(
                normalizedRoot.Length);
        }

        private static bool IsPathInsideRoot(
            string root,
            string fullPath)
        {
            string normalizedRoot =
                Path.GetFullPath(root)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            string normalizedPath =
                Path.GetFullPath(fullPath);

            return normalizedPath.StartsWith(
                normalizedRoot,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
