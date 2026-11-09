using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    // Запись событий во внешний JSON-журнал (ADR-014, этап A.6).
    //
    // Принципы:
    //   * один event — один файл (ADR-014);
    //   * атомарная публикация: .tmp -> полностью записать -> закрыть -> rename -> .json;
    //   * уникальное имя файла, существующие события не перезаписываются;
    //   * канал best-effort: ошибки не влияют на работу Revit (A.15);
    //   * замер длительности записи попадает в лог (A.17).
    public class JsonJournalWriter : IJournalWriter
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = Formatting.Indented
            };

        public void WriteAsync(
            JournalEvent journalEvent,
            JournalLocation location)
        {
            // Данные события уже собраны в DTO на Revit API-потоке (A.7),
            // файловый I/O выполняется в фоновом потоке.
            Task.Run(() => TryWrite(journalEvent, location));
        }

        public bool TryWrite(
            JournalEvent journalEvent,
            JournalLocation location)
        {
            try
            {
                if (journalEvent == null ||
                    location == null ||
                    string.IsNullOrWhiteSpace(location.JournalRoot))
                {
                    return false;
                }

                if (!JournalSettings.JsonJournalEnabled)
                {
                    Log(
                        "JOURNAL WRITE SKIP | Reason=Disabled");

                    return false;
                }

                if (journalEvent.EventId == Guid.Empty)
                {
                    journalEvent.EventId = Guid.NewGuid();
                }

                if (journalEvent.CreatedUtc == default(DateTime))
                {
                    journalEvent.CreatedUtc = DateTime.UtcNow;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                // Структура каталогов: Events\YYYY\MM\DD\Model (A.5).
                // Каталоги формируются по локальной дате создания события.
                DateTime localCreated =
                    journalEvent.CreatedUtc.ToLocalTime();

                string modelDirectory = Path.Combine(
                    location.JournalRoot,
                    "Events",
                    localCreated.Year.ToString("0000"),
                    localCreated.Month.ToString("00"),
                    localCreated.Day.ToString("00"),
                    SanitizeFolderName(GetModelId(journalEvent, location)));

                Directory.CreateDirectory(modelDirectory);

                // Уникальное имя: локальное время с точностью до мс + EventId.
                string fileName =
                    localCreated.ToString("yyyyMMddTHHmmssfff") +
                    "_" +
                    journalEvent.EventId.ToString("N") +
                    ".json";

                string finalPath = Path.Combine(
                    modelDirectory,
                    fileName);

                string tempPath = finalPath + ".tmp";

                string json = JsonConvert.SerializeObject(
                    journalEvent,
                    SerializerSettings);

                // Полностью записываем временный файл и закрываем его
                // до публикации готового файла (A.6).
                File.WriteAllText(
                    tempPath,
                    json,
                    new UTF8Encoding(false));

                if (File.Exists(finalPath))
                {
                    // Коллизия имён практически невозможна (EventId в имени),
                    // но существующие события не перезаписываются.
                    File.Delete(finalPath);
                }

                File.Move(tempPath, finalPath);

                stopwatch.Stop();

                Log(
                    "JOURNAL WRITE OK | EventId=" +
                    journalEvent.EventId +
                    " | Type=" +
                    journalEvent.EventType +
                    " | DurationMs=" +
                    stopwatch.ElapsedMilliseconds +
                    " | Path=" +
                    finalPath);

                return true;
            }
            catch (Exception ex)
            {
                // Сетевой ресурс недоступен и любые другие ошибки:
                // Sync не ломается, ошибка попадает в лог (A.15).
                Log(
                    "JOURNAL WRITE ERROR | EventId=" +
                    (journalEvent != null
                        ? journalEvent.EventId.ToString()
                        : "null") +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message);

                return false;
            }
        }


        private static string GetModelId(
            JournalEvent journalEvent,
            JournalLocation location)
        {
            if (!string.IsNullOrWhiteSpace(journalEvent.ModelId))
                return journalEvent.ModelId;

            return location.ModelId;
        }


        // Имя папки модели может содержать недопустимые для файловой
        // системы символы в экзотических случаях — защищаемся.
        private static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "UNKNOWN";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidChar, '_');
            }

            return name;
        }

        private static void Log(string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(message);
            }
            catch
            {
                // Ошибка логирования не должна влиять на запись журнала.
            }
        }
    }
}
