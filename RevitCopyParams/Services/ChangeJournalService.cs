using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using RevitCopyParams.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitCopyParams
{
    public static class ChangeJournalService
    {
        public static List<ChangeRecord> GetJournalForCurrentModel(
            Document document)
        {
            string currentModel =
                RevitLinkService.GetModelName(document);

            List<ChangeRecord> result =
                new List<ChangeRecord>();


            // Записи текущей модели
            List<ChangeRecord> currentRecords =
                ChangeExtensibleStorageService.LoadJournal(document);

            foreach (ChangeRecord record in currentRecords)
            {
                FillReadStatus(document, record);
                result.Add(record);
            }


            // Записи связанных моделей
            List<Document> linkedDocuments =
                RevitLinkService.GetLoadedLinkDocuments(document);

            foreach (Document linkDocument in linkedDocuments)
            {
                List<ChangeRecord> records =
                    ChangeExtensibleStorageService.LoadJournal(linkDocument);

                foreach (ChangeRecord record in records)
                {
                    if (!record.TargetModels.Contains(currentModel))
                        continue;

                    FillReadStatus(document, record);
                    result.Add(record);
                }
            }


            // События внешнего JSON-журнала (ADR-014)
            MergeJsonJournalEvents(
                document,
                currentModel,
                result);


            return result
                .OrderBy(x => GetStatusPriority(x.Status))
                .ThenByDescending(x => x.CreatedDate)
                .ToList();
        }


        // Объединяет в общий список события внешнего JSON-журнала,
        // адресованные текущей модели (ADR-014, окно журнала показывает
        // объединение каналов ES + JSON).
        private static void MergeJsonJournalEvents(
            Document document,
            string currentModel,
            List<ChangeRecord> result)
        {
            try
            {
                if (!JournalSettings.JsonJournalEnabled)
                    return;

                JournalLocation location =
                    JournalPathResolver.Resolve(document);

                if (location == null)
                    return;

                List<JournalEvent> events =
                    new JsonJournalReader().ReadEvents(
                        location,
                        DateTime.UtcNow.AddDays(-JournalSettings.LookbackDays),
                        currentModel);

                // Индекс идентификаторов уже загруженных ES-записей
                // для дедупликации Dual Write.
                HashSet<Guid> existingIds =
                    new HashSet<Guid>(
                        result.Select(x => x.Id));

                foreach (JournalEvent journalEvent in events)
                {
                    Guid mappedId =
                        journalEvent.RecordId.HasValue
                            ? journalEvent.RecordId.Value
                            : journalEvent.EventId;

                    // Одна и та же запись приходит по двум каналам
                    // (ES через link и JSON через сеть) — показываем один раз.
                    if (existingIds.Contains(mappedId))
                        continue;

                    ChangeRecord record =
                        JournalEventMapper.ToChangeRecord(journalEvent);

                    FillJsonStatus(record, document, journalEvent);

                    result.Add(record);

                    existingIds.Add(mappedId);
                }
            }
            catch (Exception ex)
            {
                // JSON-канал best-effort: недоступность журнала
                // не должна ломать окно журнала.
                UpdateStartupService.LogDiagnostic(
                    "JOURNAL MERGE ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
        }


        private static void FillJsonStatus(
            ChangeRecord record,
            Document document,
            JournalEvent journalEvent)
        {
            string username =
                document.Application.Username;

            if (string.Equals(
                    journalEvent.Author,
                    username,
                    StringComparison.OrdinalIgnoreCase))
            {
                record.Status =
                    ChangeStatus.Mine;

                return;
            }

            bool processed =
                JournalStateService.IsEventProcessed(
                    journalEvent.EventId) ||
                (journalEvent.RecordId.HasValue &&
                 JournalStateService.IsRecordProcessed(
                     journalEvent.RecordId.Value));

            record.Status = processed
                ? ChangeStatus.Read
                : ChangeStatus.New;
        }


        private static void FillReadStatus(
            Document document,
            ChangeRecord record)
        {
            string currentModel =
                RevitLinkService.GetModelName(document);

            if (record.SourceModel == currentModel &&
                record.Author == document.Application.Username)
            {
                record.Status =
                    ChangeStatus.Mine;

                return;
            }

            if (NotificationStateService.IsRead(
                    document,
                    record.Id))
            {
                record.Status =
                    ChangeStatus.Read;
            }
            else
            {
                record.Status =
                    ChangeStatus.New;
            }
        }



        private static int GetStatusPriority(
    ChangeStatus status)
        {
            switch (status)
            {
                case ChangeStatus.New:
                    return 0;

                case ChangeStatus.Read:
                    return 1;

                case ChangeStatus.Mine:
                    return 2;

                default:
                    return 3;
            }
        }
    }
}
