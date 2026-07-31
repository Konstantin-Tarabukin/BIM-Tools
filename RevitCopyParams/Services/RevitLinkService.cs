using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitCopyParams
{
    public static class RevitLinkService
    {

        public static string GetModelName(Document document)
        {
            if (document.IsWorkshared)
            {
                ModelPath centralPath =
                    document.GetWorksharingCentralModelPath();

                if (centralPath != null)
                {
                    string centralFile =
                        ModelPathUtils.ConvertModelPathToUserVisiblePath(
                            centralPath);

                    return System.IO.Path.GetFileNameWithoutExtension(
                        centralFile);
                }
            }

            return System.IO.Path.GetFileNameWithoutExtension(
                document.Title);
        }

        public static List<string> GetLoadedLinks(
            Document document)
        {
            List<string> links =
                new List<string>();

            FilteredElementCollector collector =
                new FilteredElementCollector(document);

            foreach (RevitLinkInstance link in
                collector.OfClass(typeof(RevitLinkInstance)))
            {
                Document linkDocument =
                    link.GetLinkDocument();

                if (linkDocument == null)
                    continue;

                string modelName =
                    GetModelName(linkDocument);

                links.Add(modelName);
            }

            return links;
        }




        public static List<Document> GetLoadedLinkDocuments(
    Document document)
        {
            List<Document> links =
                new List<Document>();


            FilteredElementCollector collector =
                new FilteredElementCollector(document);


            foreach (RevitLinkInstance link in
                collector.OfClass(typeof(RevitLinkInstance)))
            {
                Document linkDocument =
                    link.GetLinkDocument();


                if (linkDocument == null)
                    continue;


                links.Add(linkDocument);
            }


            return links;
        }
    }
}