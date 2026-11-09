using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;

namespace RevitCopyParams.Services
{
    // Публикация записи журнала во внешний JSON-журнал (Dual Write, A.8).
    //
    // Во время POC каждая запись сохраняется одновременно:
    //   ChangeRecord -> Extensible Storage (как раньше)
    //                -> JsonJournalWriter (новый канал)
    //
    // Внешний канал best-effort: любые ошибки пишутся в лог
    // и не влияют на работу пользователя (A.15).
    //
    // Вызывается из Revit API-потока (окно журнала), сама запись
    // уходит в background (A.7).
    public static class JournalPublishService
    {
        private static readonly JsonJournalWriter Writer =
            new JsonJournalWriter();

        public static void PublishRecord(
            Document document,
            ChangeRecord record,
            string eventType)
        {
            try
            {
                if (!JournalSettings.JsonJournalEnabled)
                    return;

                if (record == null || document == null)
                    return;

                // Resolve использует Revit API — здесь мы на API-потоке.
                JournalLocation location =
                    JournalPathResolver.Resolve(document);

                if (location == null)
                {
                    UpdateStartupService.LogDiagnostic(
                        "JOURNAL PUBLISH SKIP | Reason=NoJournalLocation | RecordId=" +
                        record.Id);

                    return;
                }

                string author = record.Author;

                if (string.IsNullOrWhiteSpace(author))
                    author = document.Application.Username;

                // В background передаётся уже сформированный JournalEvent (A.7).
                JournalEvent journalEvent =
                    JournalEventMapper.ToJournalEvent(
                        record,
                        eventType,
                        location,
                        author);

                Writer.WriteAsync(journalEvent, location);

                UpdateStartupService.LogDiagnostic(
                    "JOURNAL PUBLISH QUEUED | EventId=" +
                    journalEvent.EventId +
                    " | Type=" +
                    eventType +
                    " | SourceModel=" +
                    journalEvent.ModelName +
                    " | Targets=" +
                    record.TargetModelsText);
            }
            catch (Exception ex)
            {
                // Ошибка публикации не должна прерывать действие пользователя.
                UpdateStartupService.LogDiagnostic(
                    "JOURNAL PUBLISH ERROR | " +
                    ex.GetType().FullName +
                    " | " +
                    ex.Message);
            }
        }
    }
}
