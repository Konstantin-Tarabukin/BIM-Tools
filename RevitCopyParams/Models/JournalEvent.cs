using System;
using System.Collections.Generic;

namespace RevitCopyParams.Models
{
    // Типы событий внешнего журнала (ADR-014).
    // Список будет расширяться (Sprint 5 — привязка элементов).
    public static class JournalEventTypes
    {
        public const string RecordCreated = "RecordCreated";

        public const string RecordEdited = "RecordEdited";
    }

    // Контракт события внешнего JSON-журнала (ADR-014, этап A.1).
    //
    // Каждое событие сохраняется в отдельном файле:
    //   _BIMToolsJournal\Events\YYYY\MM\DD\Model\<timestamp>_<eventId>.json
    //
    // Событие не зависит от backend: в будущем JSON может быть
    // заменён на Journal Service + SQLite без изменения модели.
    public class JournalEvent
    {
        // Текущая версия схемы JSON.
        // Reader пропускает события с другой версией схемы.
        public const int CurrentSchemaVersion = 1;

        public JournalEvent()
        {
            TargetModels = new List<string>();

            Elements = new List<ChangeElementReference>();
        }

        public int SchemaVersion { get; set; }

        // Уникальный идентификатор события (Guid.NewGuid).
        // Никаких клиентских счётчиков MAX(Id)+1 (ADR-014).
        public Guid EventId { get; set; }

        // Идентификатор проекта (имя корневой папки проекта).
        public string ProjectId { get; set; }

        // Короткий идентификатор модели (папка модели, например AR).
        public string ModelId { get; set; }

        // Имя модели (имя central-файла без расширения).
        public string ModelName { get; set; }

        public string Author { get; set; }

        // Время создания события в UTC.
        // Каталоги событий формируются по локальной дате.
        public DateTime CreatedUtc { get; set; }

        public string EventType { get; set; }

        public string Message { get; set; }

        // Идентификатор исходной записи ChangeRecord (Extensible Storage).
        // Связывает событие внешнего журнала с legacy-записью и позволяет
        // исключить дубли одной записи из двух каналов (ES + JSON)
        // в окне журнала и в уведомлениях.
        public Guid? RecordId { get; set; }

        // Модели-получатели (имена central-файлов).
        // Сопоставление такое же, как в legacy ChangeRecord.TargetModels.
        public List<string> TargetModels { get; set; }

        // Привязанные элементы модели (Sprint 5, этап A.2).
        // В POC список пустой, но контракт уже поддерживает его.
        public List<ChangeElementReference> Elements { get; set; }
    }
}
