using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RevitCopyParams.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace RevitCopyParams.Services
{
    // Чтение событий из внешнего JSON-журнала (ADR-014, этап A.9).
    //
    // Принципы:
    //   * читаются только каталоги актуального периода (LookbackDays);
    //   * .tmp-файлы игнорируются (A.6);
    //   * битые файлы и файлы с другой SchemaVersion пропускаются
    //     с записью в лог — Revit не падает (A.15);
    //   * недоступность сетевого ресурса не является ошибкой (A.15);
    //   * SearchOption.AllDirectories запрещён (A.5):
    //     перебираются только датные каталоги и их папки моделей.
    public class JsonJournalReader : IJournalReader
    {
        private static readonly JsonSerializerSettings DeserializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };

        public List<JournalEvent> ReadEvents(
            JournalLocation location,
            DateTime utcFromInclusive,
            string targetModelName)
        {
            List<JournalEvent> result =
                new List<JournalEvent>();

            if (location == null ||
                string.IsNullOrWhiteSpace(location.JournalRoot))
            {
                return result;
            }

            string eventsRoot = Path.Combine(
                location.JournalRoot,
                "Events");

            if (!Directory.Exists(eventsRoot))
            {
                // Журнал ещё не создан или сетевой ресурс недоступен —
                // нормальная ситуация для POC (A.15).
                Log(
                    "JOURNAL READ SKIP | Reason=EventsDirectoryNotFound | Path=" +
                    eventsRoot);

                return result;
            }

            try
            {
                // Каталоги событий формируются по локальной дате (A.5),
                // период чтения — от utcFromInclusive до текущего дня.
                DateTime localFrom =
                    utcFromInclusive.ToLocalTime().Date;

                DateTime localToday = DateTime.Today;

                for (DateTime day = localFrom;
                     day <= localToday;
                     day = day.AddDays(1))
                {
                    string dayDirectory = Path.Combine(
                        eventsRoot,
                        day.Year.ToString("0000"),
                        day.Month.ToString("00"),
                        day.Day.ToString("00"));

                    if (!Directory.Exists(dayDirectory))
                        continue;

                    foreach (string modelDirectory in
                        Directory.GetDirectories(dayDirectory))
                    {
                        ReadModelDirectory(
                            modelDirectory,
                            targetModelName,
                            result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Сетевая ошибка на уровне перечисления каталогов:
                // возвращаем то, что успели прочитать.
                Log(
                    "JOURNAL READ ERROR | Root=" +
                    eventsRoot +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message);
            }

            result.Sort(
                (a, b) => a.CreatedUtc.CompareTo(b.CreatedUtc));

            return result;
        }


        private void ReadModelDirectory(
            string modelDirectory,
            string targetModelName,
            List<JournalEvent> result)
        {
            string[] files;

            try
            {
                // Pattern "*.json" не захватывает "*.json.tmp" (A.6).
                // Только TopDirectoryOnly — без обхода всей истории (A.5).
                files = Directory.GetFiles(
                    modelDirectory,
                    "*.json",
                    SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL READ ERROR | Directory=" +
                    modelDirectory +
                    " | Message=" +
                    ex.Message);

                return;
            }

            foreach (string file in files)
            {
                JournalEvent journalEvent =
                    TryReadFile(file);

                if (journalEvent == null)
                    continue;

                if (!IsTargetedAt(journalEvent, targetModelName))
                    continue;

                result.Add(journalEvent);
            }
        }


        private JournalEvent TryReadFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);

                JournalEvent journalEvent =
                    JsonConvert.DeserializeObject<JournalEvent>(
                        json,
                        DeserializerSettings);

                if (journalEvent == null ||
                    journalEvent.EventId == Guid.Empty)
                {
                    Log(
                        "JOURNAL READ SKIP | Reason=EmptyOrInvalid | File=" +
                        Path.GetFileName(filePath));

                    return null;
                }

                if (journalEvent.SchemaVersion !=
                    JournalEvent.CurrentSchemaVersion)
                {
                    Log(
                        "JOURNAL READ SKIP | Reason=SchemaVersionMismatch | File=" +
                        Path.GetFileName(filePath) +
                        " | Found=" +
                        journalEvent.SchemaVersion +
                        " | Expected=" +
                        JournalEvent.CurrentSchemaVersion);

                    return null;
                }

                // Обработка неполных событий (A.9): отсутствующие
                // коллекции нормализуем, обязательные поля не требуем.
                if (journalEvent.TargetModels == null)
                {
                    journalEvent.TargetModels =
                        new List<string>();
                }

                if (journalEvent.Elements == null)
                {
                    journalEvent.Elements =
                        new List<ChangeElementReference>();
                }

                return journalEvent;
            }
            catch (JsonException ex)
            {
                // Битый JSON: log -> skip -> повторная попытка при
                // следующем опросе не требуется, файл уже опубликован.
                Log(
                    "JOURNAL READ BAD JSON | File=" +
                    filePath +
                    " | Message=" +
                    ex.Message);

                return null;
            }
            catch (IOException ex)
            {
                // Файл мог быть заблокирован в момент чтения:
                // пропускаем, при следующем опросе прочитается (A.9).
                Log(
                    "JOURNAL READ IO | File=" +
                    filePath +
                    " | Message=" +
                    ex.Message);

                return null;
            }
            catch (Exception ex)
            {
                Log(
                    "JOURNAL READ ERROR | File=" +
                    filePath +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message);

                return null;
            }
        }


        private static bool IsTargetedAt(
            JournalEvent journalEvent,
            string targetModelName)
        {
            if (string.IsNullOrEmpty(targetModelName))
                return false;

            foreach (string target in journalEvent.TargetModels)
            {
                if (string.Equals(
                        target,
                        targetModelName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Log(string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(message);
            }
            catch
            {
                // Ошибка логирования не должна влиять на чтение журнала.
            }
        }
    }
}
