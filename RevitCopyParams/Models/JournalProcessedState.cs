using System;
using System.Collections.Generic;

namespace RevitCopyParams.Models
{
    // Локальное состояние обработки внешних JSON-событий (ADR-014, этап A.11).
    //
    // Хранится только у пользователя:
    //   %AppData%\BIMTools\RevitCopyParams\JournalState.json
    //
    // Общее состояние в сетевой папке проекта не создаётся:
    // прочтение — персональное состояние (ADR-005).
    public class JournalProcessedState
    {
        public const int CurrentSchemaVersion = 1;

        public JournalProcessedState()
        {
            Processed = new List<JournalProcessedEntry>();
        }

        public int SchemaVersion { get; set; }

        public List<JournalProcessedEntry> Processed { get; set; }
    }

    // Одна запись об обработанном событии.
    public class JournalProcessedEntry
    {
        // Идентификатор события внешнего журнала.
        public Guid EventId { get; set; }

        // Идентификатор legacy-записи ChangeRecord (если событие
        // создано через Dual Write). Позволяет исключить повторное
        // уведомление по каналу Extensible Storage.
        public Guid? RecordId { get; set; }

        // Момент обработки в UTC — используется для очистки старых записей.
        public DateTime ProcessedUtc { get; set; }
    }
}
