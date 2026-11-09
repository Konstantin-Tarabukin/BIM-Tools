using Newtonsoft.Json;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.IO;

namespace RevitCopyParams.Services
{
    // Локальное (персональное) состояние обработки внешних JSON-событий
    // (ADR-014, этап A.11).
    //
    // Требования этапа A.11:
    //   * состояние переживает перезапуск Revit — файл на диске;
    //   * переживает временную недоступность сетевого журнала —
    //     файл локальный, не в папке проекта;
    //   * защищено от повреждения — атомарная запись, битый файл
    //     переименовывается и состояние начинается заново;
    //   * старые EventId периодически очищаются.
    //
    // Класс потокобезопасен: обращается к нему и polling
    // (background-поток), и окно журнала (UI-поток).
    public static class JournalStateService
    {
        // Срок хранения записей состояния.
        private const int StateRetentionDays = 14;

        // Максимальное число записей — защита от бесконечного роста.
        private const int MaxEntries = 2000;

        private static readonly object LockObject = new object();

        private static JournalProcessedState state;

        private static bool loaded;


        public static bool IsEventProcessed(Guid eventId)
        {
            lock (LockObject)
            {
                EnsureLoaded();

                foreach (JournalProcessedEntry entry in state.Processed)
                {
                    if (entry.EventId == eventId)
                        return true;
                }

                return false;
            }
        }


        public static bool IsRecordProcessed(Guid recordId)
        {
            lock (LockObject)
            {
                EnsureLoaded();

                foreach (JournalProcessedEntry entry in state.Processed)
                {
                    if (entry.RecordId.HasValue &&
                        entry.RecordId.Value == recordId)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        // Помечает событие обработанным. Вызывается сразу после
        // отправки уведомления на показ (решение POC: дедупликация
        // "при показе" — повторные уведомления исключены, A.14).
        public static void MarkProcessed(
            Guid eventId,
            Guid? recordId)
        {
            lock (LockObject)
            {
                EnsureLoaded();

                foreach (JournalProcessedEntry entry in state.Processed)
                {
                    if (entry.EventId == eventId)
                        return;
                }

                state.Processed.Add(
                    new JournalProcessedEntry
                    {
                        EventId = eventId,
                        RecordId = recordId,
                        ProcessedUtc = DateTime.UtcNow
                    });

                Cleanup(state);

                Save();
            }
        }


        private static void EnsureLoaded()
        {
            if (loaded)
                return;

            state = Load();

            loaded = true;
        }


        private static JournalProcessedState Load()
        {
            string path =
                BIMToolsPaths.JournalStateJsonPath;

            try
            {
                if (!File.Exists(path))
                    return new JournalProcessedState();

                JournalProcessedState loadedState =
                    JsonConvert.DeserializeObject<JournalProcessedState>(
                        File.ReadAllText(path));

                if (loadedState == null ||
                    loadedState.Processed == null)
                {
                    return new JournalProcessedState();
                }

                return loadedState;
            }
            catch (Exception ex)
            {
                // Повреждённое состояние не должно ломать Revit:
                // переносим битый файл и начинаем с чистого состояния.
                Log(
                    "JOURNAL STATE CORRUPT | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);

                try
                {
                    if (File.Exists(path))
                    {
                        // Уникальный суффикс: две сессии Revit могут
                        // обнаружить повреждение в одну и ту же секунду.
                        string backupPath =
                            path +
                            ".corrupt-" +
                            DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                            "-" +
                            Guid.NewGuid().ToString("N").Substring(0, 8);

                        File.Move(path, backupPath);
                    }
                }
                catch
                {
                    // Не смогли переименовать — просто перезапишем позже.
                }

                return new JournalProcessedState();
            }
        }


        private static void Save()
        {
            string path =
                BIMToolsPaths.JournalStateJsonPath;

            string tempPath = path + ".tmp";

            try
            {
                Directory.CreateDirectory(
                    BIMToolsPaths.BIMToolsDirectory);

                string json = JsonConvert.SerializeObject(
                    state,
                    Formatting.Indented);

                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    // Атомарная замена существующего файла.
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL STATE SAVE ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);

                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Ничего критичного: состояние перезапишется
                    // при следующей отметке.
                }
            }
        }


        private static void Cleanup(JournalProcessedState stateToClean)
        {
            DateTime cutoffUtc =
                DateTime.UtcNow.AddDays(-StateRetentionDays);

            stateToClean.Processed.RemoveAll(
                entry => entry.ProcessedUtc < cutoffUtc);

            if (stateToClean.Processed.Count <= MaxEntries)
                return;

            // Срезаем самые старые записи при переполнении.
            stateToClean.Processed.Sort(
                (a, b) => a.ProcessedUtc.CompareTo(b.ProcessedUtc));

            stateToClean.Processed.RemoveRange(
                0,
                stateToClean.Processed.Count - MaxEntries);
        }

        private static void Log(string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(message);
            }
            catch
            {
                // Ошибка логирования не должна влиять на работу.
            }
        }
    }
}
