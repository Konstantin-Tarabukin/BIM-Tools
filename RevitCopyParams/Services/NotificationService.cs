using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCopyParams.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Interop;


namespace RevitCopyParams
{
    public class NotificationService
    {

        public void CheckNotifications(
            Document document,
            string eventName)
        {



            string currentModel =
                RevitLinkService.GetModelName(document);


            List<ChangeRecord> notifications =
                new List<ChangeRecord>();


            List<Document> linkedDocuments =
                RevitLinkService.GetLoadedLinkDocuments(
                    document);





            foreach (Document linkDocument in linkedDocuments)
            {
                List<ChangeRecord> records =
                    ChangeExtensibleStorageService.LoadJournal(
                        linkDocument);




                foreach (ChangeRecord record in records)
                {



                    if (!record.TargetModels.Contains(currentModel))
                        continue;

                    if (NotificationStateService.IsRead(
                            document,
                            record.Id))
                    {
                        continue;
                    }




                    notifications.Add(record);
                }
            }



            if (notifications.Count == 0)
            {
                return;
            }



            NotificationWindow window =
                new NotificationWindow(notifications);

            WindowInteropHelper helper =
                new WindowInteropHelper(window);

            helper.Owner =
                Process.GetCurrentProcess().MainWindowHandle;

            bool? userConfirmed = window.ShowDialog();


            if (userConfirmed == true)
            {
                foreach (ChangeRecord record in notifications)
                {
                    NotificationStateService.MarkAsRead(
                        document,
                        record.Id);
                }
            }

        }
    }
}