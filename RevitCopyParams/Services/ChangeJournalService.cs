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

            result.AddRange(currentRecords);


            // Записи связанных моделей
            List<Document> linkedDocuments =
                RevitLinkService.GetLoadedLinkDocuments(document);

            foreach (Document linkDocument in linkedDocuments)
            {
                List<ChangeRecord> records =
                    ChangeExtensibleStorageService.LoadJournal(linkDocument);

                foreach (ChangeRecord record in records)
                {
                    if (record.TargetModels.Contains(currentModel))
                    {
                        result.Add(record);
                    }
                }
            }


            return result
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }
    }
}