using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitCopyParams
{
    public static class NotificationStateService
    {

        private static readonly Guid SchemaGuid =
            new Guid("B6D2A2E4-1F8C-4A6D-8C91-3F8D9E4A52B1");


        private static Schema GetSchema()
        {
            Schema schema =
                Schema.Lookup(SchemaGuid);

            if (schema != null)
                return schema;


            SchemaBuilder builder =
                new SchemaBuilder(SchemaGuid);


            builder.SetSchemaName(
                "BIMToolsNotificationState");


            builder.AddSimpleField(
                "ReadIdsJson",
                typeof(string));


            schema =
                builder.Finish();


            return schema;
        }



        private static DataStorage GetOrCreateDataStorage(
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


                if (entity.IsValid())
                    return storage;
            }



            using (Transaction transaction =
                new Transaction(
                    document,
                    "Create Notification State"))
            {
                transaction.Start();


                DataStorage storage =
                    DataStorage.Create(document);


                Entity entity =
                    new Entity(schema);


                entity.Set(
                    "ReadIdsJson",
                    "[]");


                storage.SetEntity(entity);


                transaction.Commit();


                return storage;
            }
        }



        public static List<Guid> LoadReadIds(
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
                    entity.Get<string>(
                        "ReadIdsJson");



                if (string.IsNullOrWhiteSpace(json))
                    return new List<Guid>();


                try
                {
                    return JsonConvert
                        .DeserializeObject<List<Guid>>(json)
                        ?? new List<Guid>();
                }
                catch
                {
                    return new List<Guid>();
                }
            }


            return new List<Guid>();
        }




        public static void SaveReadIds(
            Document document,
            List<Guid> ids)


        {

            DataStorage storage =
                GetOrCreateDataStorage(document);


            Schema schema =
                GetSchema();


            using (Transaction transaction =
                new Transaction(
                    document,
                    "Save Notification State"))
            {
                transaction.Start();


                Entity entity =
                    storage.GetEntity(schema);


                if (!entity.IsValid())
                    entity =
                        new Entity(schema);



                entity.Set(
                    "ReadIdsJson",
                    JsonConvert.SerializeObject(ids));



                storage.SetEntity(entity);


                transaction.Commit();

;
            }
        }




        public static bool IsRead(
            Document document,
            Guid id)
        {
            List<Guid> ids =
                LoadReadIds(document);


            return ids.Contains(id);
        }





        public static void MarkAsRead(
            Document document,
            Guid id)
        {

            List<Guid> ids =
                LoadReadIds(document);


            if (ids.Contains(id))
                return;


            ids.Add(id);


            SaveReadIds(
                document,
                ids);

            List<Guid> checkIds =
                LoadReadIds(document);




        }

    }
}