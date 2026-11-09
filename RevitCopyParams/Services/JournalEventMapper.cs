using RevitCopyParams.Models;
using System;
using System.Collections.Generic;

namespace RevitCopyParams.Services
{
    // Преобразование между legacy ChangeRecord (Extensible Storage)
    // и контрактом внешнего журнала JournalEvent (ADR-014, этап A.8).
    //
    // Все преобразования выполняются без обращения к Revit API,
    // поэтому класс пригоден для офлайн-тестирования (этап A.12).
    public static class JournalEventMapper
    {
        // ChangeRecord -> JournalEvent (Dual Write).
        // Вызывается на Revit API-потоке: author уже получен из документа.
        public static JournalEvent ToJournalEvent(
            ChangeRecord record,
            string eventType,
            JournalLocation location,
            string author)
        {
            return new JournalEvent
            {
                SchemaVersion = JournalEvent.CurrentSchemaVersion,
                EventId = Guid.NewGuid(),
                ProjectId = location != null ? location.ProjectId : null,
                ModelId = location != null ? location.ModelId : null,
                ModelName = record.SourceModel,
                Author = author,
                CreatedUtc = DateTime.UtcNow,
                EventType = eventType,
                Message = record.Description,
                RecordId = record.Id,
                TargetModels = record.TargetModels != null
                    ? new List<string>(record.TargetModels)
                    : new List<string>(),
                Elements = new List<ChangeElementReference>()
            };
        }


        // JournalEvent -> ChangeRecord (окно журнала и уведомления).
        // Статус (Mine/New/Read) выставляется вызывающим кодом.
        public static ChangeRecord ToChangeRecord(
            JournalEvent journalEvent)
        {
            return new ChangeRecord
            {
                // Dual Write-событие наследует идентификатор
                // legacy-записи — дедупликация по Id в окне журнала.
                Id = journalEvent.RecordId.HasValue
                    ? journalEvent.RecordId.Value
                    : journalEvent.EventId,
                Description = journalEvent.Message,
                TargetModels = journalEvent.TargetModels != null
                    ? new List<string>(journalEvent.TargetModels)
                    : new List<string>(),
                CreatedDate = journalEvent.CreatedUtc.ToLocalTime(),
                Author = journalEvent.Author,
                SourceModel = journalEvent.ModelName,
                IsEdited = string.Equals(
                    journalEvent.EventType,
                    JournalEventTypes.RecordEdited,
                    StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}
