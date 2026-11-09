using Newtonsoft.Json;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    public class JsonJournalWriter : IJournalWriter, IDisposable
    {
        private sealed class JournalWriteItem
        {
            public string JournalRoot { get; set; }
            public JournalEvent Event { get; set; }
        }

        private readonly BlockingCollection<JournalWriteItem> _queue;
        private readonly CancellationTokenSource _cancellation;
        private readonly Task _worker;
        private bool _disposed;

        public JsonJournalWriter()
        {
            _queue = new BlockingCollection<JournalWriteItem>(256);
            _cancellation = new CancellationTokenSource();
            _worker = Task.Factory.StartNew(
                WorkerLoop,
                _cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Enqueue(
            string journalRoot,
            JournalEvent journalEvent)
        {
            if (_disposed)
                return;

            if (string.IsNullOrWhiteSpace(journalRoot))
                return;

            if (journalEvent == null)
                return;

            try
            {
                bool accepted = _queue.TryAdd(
                    new JournalWriteItem
                    {
                        JournalRoot = journalRoot,
                        Event = journalEvent
                    });

                if (!accepted)
                {
                    Log(
                        "JSON WRITE QUEUE FULL: event=" +
                        journalEvent.EventId);
                }
            }
            catch (InvalidOperationException)
            {
                // Service is shutting down. The source ES record remains intact
                // and will be republished on the next successful Sync.
            }
        }

        private void WorkerLoop()
        {
            try
            {
                foreach (JournalWriteItem item in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        WriteEvent(
                            item.JournalRoot,
                            item.Event);
                    }
                    catch (Exception ex)
                    {
                        Log(
                            "JSON WRITE ERROR: " +
                            ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void WriteEvent(
            string journalRoot,
            JournalEvent journalEvent)
        {
            string modelDirectory =
                JsonJournalPathService.SanitizePathSegment(
                    journalEvent.ModelId);

            DateTime date = journalEvent.CreatedUtc.ToUniversalTime();

            string dayDirectory = Path.Combine(
                journalRoot,
                date.ToString("yyyy"),
                date.ToString("MM"),
                date.ToString("dd"),
                modelDirectory);

            Directory.CreateDirectory(dayDirectory);

            string fileName = string.Format(
                "{0}_{1:N}.json",
                date.ToString("yyyyMMddTHHmmssfffZ"),
                journalEvent.EventId);

            string targetPath =
                Path.Combine(dayDirectory, fileName);

            // A normal, non-edited event is immutable. Avoid touching its file
            // on every subsequent Sync.
            if (!journalEvent.IsEdited && File.Exists(targetPath))
                return;

            string json = JsonConvert.SerializeObject(
                journalEvent,
                Formatting.Indented);

            string tempPath =
                targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                using (StreamWriter writer = new StreamWriter(
                    stream,
                    new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush();
                }

                PublishFinishedFile(
                    tempPath,
                    targetPath);

                Log(
                    "JSON WRITE OK: " +
                    targetPath);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        private static void PublishFinishedFile(
            string tempPath,
            string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                File.Move(
                    tempPath,
                    targetPath);
                return;
            }

            try
            {
                File.Replace(
                    tempPath,
                    targetPath,
                    null,
                    true);
            }
            catch
            {
                // Some SMB/file-server configurations may not support
                // File.Replace. For POC fall back to delete + move.
                File.Delete(targetPath);
                File.Move(
                    tempPath,
                    targetPath);
            }
        }

        private static void TryDelete(
            string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            bool workerStopped = false;

            try
            {
                _queue.CompleteAdding();
                workerStopped = _worker.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
            }

            if (workerStopped)
            {
                _cancellation.Cancel();
                _queue.Dispose();
                _cancellation.Dispose();
            }
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
