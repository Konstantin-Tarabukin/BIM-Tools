using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RevitCopyParams.Services
{
    public class JsonJournalPublisher
    {
        private readonly IJournalWriter _writer;

        public JsonJournalPublisher(
            IJournalWriter writer)
        {
            _writer = writer;
        }

        public void PublishForDocument(
            Document document)
        {
            string journalRoot;
            string projectId;
            string modelId;

            if (!JsonJournalPathService.TryGetJournalRoot(
                    document,
                    out journalRoot,
                    out projectId,
                    out modelId))
            {
                return;
            }

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            DateTime journalEnabledSinceUtc =
                JsonJournalPathService.GetEnabledSinceUtc(
                    journalRoot);

            List<ChangeRecord> records =
                ChangeExtensibleStorageService.LoadJournal(
                    document);

            int queued = 0;

            foreach (ChangeRecord record in records)
            {
                if (record == null)
                    continue;

                if (!string.Equals(
                        record.SourceModel,
                        modelId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                DateTime recordCreatedUtc =
                    record.CreatedDate.ToUniversalTime();

                DateTime recordModifiedUtc =
                    record.ModifiedDate.HasValue
                        ? record.ModifiedDate.Value.ToUniversalTime()
                        : DateTime.MinValue;

                if (journalEnabledSinceUtc != DateTime.MinValue &&
                    recordCreatedUtc < journalEnabledSinceUtc &&
                    recordModifiedUtc < journalEnabledSinceUtc)
                {
                    continue;
                }

                JournalEvent journalEvent =
                    JournalEventFactory.FromChangeRecord(
                        document,
                        projectId,
                        record);

                _writer.Enqueue(
                    journalRoot,
                    journalEvent);

                queued++;
            }

            stopwatch.Stop();

            Log(
                "PUBLISH: project=" +
                projectId +
                ", model=" +
                modelId +
                ", records=" +
                records.Count +
                ", queued=" +
                queued +
                ", enabledSinceUtc=" +
                journalEnabledSinceUtc.ToString("o") +
                ", durationMs=" +
                stopwatch.ElapsedMilliseconds);
        }

        private static void Log(string message)
        {
            try
            {
                BIMToolsPaths.EnsureDirectories();
                File.AppendAllText(
                    BIMToolsPaths.JournalLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " | " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
