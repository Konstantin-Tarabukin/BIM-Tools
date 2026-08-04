using Autodesk.Revit.DB;
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


            return result
                .OrderBy(x => GetStatusPriority(x.Status))
                .ThenByDescending(x => x.CreatedDate)
                .ToList();
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