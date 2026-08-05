using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using Newtonsoft.Json;

using System.Collections.Generic;
namespace RevitCopyParams
{
    public static class ChangeExtensibleStorageService
    {



        private static Schema GetSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);

            if (schema != null)
                return schema;

            SchemaBuilder builder =
                new SchemaBuilder(SchemaGuid);

            builder.SetSchemaName("BIMToolsChangeJournal");

            builder.AddSimpleField(
                "JournalJson",
                typeof(string));



            schema = builder.Finish();

            return schema;
        }


        private static DataStorage GetOrCreateDataStorage(
    Document document)
        {
            Schema schema = GetSchema();

            FilteredElementCollector collector =
                new FilteredElementCollector(document);

            foreach (DataStorage storage in collector.OfClass(typeof(DataStorage)))
            {
                Entity entity = storage.GetEntity(schema);

                if (entity.IsValid())
                    return storage;
            }

            using (Transaction transaction =
                new Transaction(document, "Create Change Journal"))
            {
                transaction.Start();

                DataStorage storage =
                    DataStorage.Create(document);

                Entity entity =
                    new Entity(schema);

                entity.Set(
                    "JournalJson",
                    "[]");



                storage.SetEntity(entity);

                transaction.Commit();

                return storage;
            }
        }
        private static string SerializeJournal(
    List<ChangeRecord> journal)
        {
            return JsonConvert.SerializeObject(
                journal,
                Formatting.Indented);
        }


        private static List<ChangeRecord> DeserializeJournal(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<ChangeRecord>();

            try
            {
                List<ChangeRecord> journal =
                    JsonConvert.DeserializeObject<List<ChangeRecord>>(json);

                return journal ?? new List<ChangeRecord>();
            }
            catch
            {
                return new List<ChangeRecord>();
            }
        }

        private static readonly Guid SchemaGuid =

    new Guid("8F6E8F6C-93C7-4D5F-9F0A-7C9F58A34E71");
        public static void SaveJournal(
            Document document,
            List<ChangeRecord> journal)
        {
            DataStorage storage =
                GetOrCreateDataStorage(document);

            Schema schema =
                GetSchema();

            using (Transaction transaction =
                new Transaction(document, "Save Change Journal"))
            {
                transaction.Start();

                string json =
                    SerializeJournal(journal);

                Entity entity =
                    new Entity(schema);

                entity.Set(
                    "JournalJson",
                    json);

                storage.SetEntity(entity);

                transaction.Commit();
            }
        }


        public static List<ChangeRecord> LoadJournal(
            Document document)
        {
            Schema schema =
                GetSchema();

            FilteredElementCollector collector =
                new FilteredElementCollector(document);

            foreach (DataStorage storage in collector
                .OfClass(typeof(DataStorage)))
            {
                Entity entity =
                    storage.GetEntity(schema);

                if (!entity.IsValid())
                    continue;

                string json =
                    entity.Get<string>("JournalJson");

                return DeserializeJournal(json);
            }

            return new List<ChangeRecord>();
        }

        public static void AddRecord(
    Document document,
    ChangeRecord record)
        {
            List<ChangeRecord> journal =
                LoadJournal(document);

            journal.Add(record);

            SaveJournal(
                document,
                journal);
        }


        public static void UpdateRecord(
    Document document,
    ChangeRecord updatedRecord)
        {
            List<ChangeRecord> journal =
                LoadJournal(document);

            for (int i = 0; i < journal.Count; i++)
            {
                if (journal[i].Id != updatedRecord.Id)
                    continue;

                journal[i] = updatedRecord;

                SaveJournal(
                    document,
                    journal);

                return;
            }
        }






    }
}