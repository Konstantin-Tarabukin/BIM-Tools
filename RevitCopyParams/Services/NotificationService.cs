using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCopyParams.Services;
using RevitCopyParams.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string currentModel = "UNKNOWN";

            try
            {
                UpdateStartupService.LogDiagnostic(
                    "NOTIFICATION CHECK START | Event=" +
                    eventName);

                currentModel =
                    RevitLinkService.GetModelName(
                        document);

                UpdateStartupService.LogDiagnostic(
                    "HOST MODEL | Model=" +
                    currentModel);

                List<ChangeRecord> notifications =
                    new List<ChangeRecord>();

                List<Document> linkedDocuments =
                    RevitLinkService.GetLoadedLinkDocuments(
                        document);

                UpdateStartupService.LogDiagnostic(
                    "LINKS FOUND | Model=" +
                    currentModel +
                    " | Count=" +
                    linkedDocuments.Count);

                foreach (Document linkDocument in linkedDocuments)
                {
                    string linkModel = "UNKNOWN";

                    try
                    {
                        linkModel =
                            RevitLinkService.GetModelName(
                                linkDocument);

                        UpdateStartupService.LogDiagnostic(
                            "LINK START | Host=" +
                            currentModel +
                            " | Link=" +
                            linkModel);

                        List<ChangeRecord> records =
                            ChangeExtensibleStorageService.LoadJournal(
                                linkDocument);

                        UpdateStartupService.LogDiagnostic(
                            "JOURNAL LOADED | Host=" +
                            currentModel +
                            " | Link=" +
                            linkModel +
                            " | Records=" +
                            records.Count);

                        foreach (ChangeRecord record in records)
                        {
                            UpdateStartupService.LogDiagnostic(
                                "RECORD | Link=" +
                                linkModel +
                                " | Id=" +
                                record.Id +
                                " | Author=" +
                                record.Author +
                                " | SourceModel=" +
                                record.SourceModel +
                                " | Targets=" +
                                record.TargetModelsText);

                            if (!record.TargetModels.Contains(
                                    currentModel))
                            {
                                UpdateStartupService.LogDiagnostic(
                                    "RECORD SKIP | Reason=TargetModel mismatch | Id=" +
                                    record.Id);

                                continue;
                            }

                            if (NotificationStateService.IsRead(
                                    document,
                                    record.Id))
                            {
                                UpdateStartupService.LogDiagnostic(
                                    "RECORD SKIP | Reason=Already read | Id=" +
                                    record.Id);

                                continue;
                            }

                            notifications.Add(record);

                            UpdateStartupService.LogDiagnostic(
                                "NOTIFICATION ADDED | Id=" +
                                record.Id);
                        }

                        UpdateStartupService.LogDiagnostic(
                            "LINK END | Host=" +
                            currentModel +
                            " | Link=" +
                            linkModel);
                    }
                    catch (Exception ex)
                    {
                        UpdateStartupService.LogDiagnostic(
                            "LINK EXCEPTION | Host=" +
                            currentModel +
                            " | Link=" +
                            linkModel +
                            " | Type=" +
                            ex.GetType().FullName +
                            " | Message=" +
                            ex.Message +
                            " | StackTrace=" +
                            ex.StackTrace);

                        throw;
                    }
                }

                UpdateStartupService.LogDiagnostic(
                    "NOTIFICATIONS TOTAL | Host=" +
                    currentModel +
                    " | Count=" +
                    notifications.Count);

                if (notifications.Count == 0)
                {
                    UpdateStartupService.LogDiagnostic(
                        "NOTIFICATION CHECK END | Result=None");

                    return;
                }

                UpdateStartupService.LogDiagnostic(
                    "OPEN NOTIFICATION WINDOW | Count=" +
                    notifications.Count);

                NotificationWindow window =
                    new NotificationWindow(
                        notifications);

                WindowInteropHelper helper =
                    new WindowInteropHelper(window);

                helper.Owner =
                    Process.GetCurrentProcess()
                        .MainWindowHandle;

                bool? userConfirmed =
                    window.ShowDialog();

                UpdateStartupService.LogDiagnostic(
                    "NOTIFICATION WINDOW CLOSED | Result=" +
                    userConfirmed);

                if (userConfirmed == true)
                {
                    NotificationStateService.MarkAsRead(
                        document,
                        notifications.Select(
                            x => x.Id));

                    UpdateStartupService.LogDiagnostic(
                        "NOTIFICATIONS MARKED AS READ | Count=" +
                        notifications.Count);
                }

                UpdateStartupService.LogDiagnostic(
                    "NOTIFICATION CHECK END | Result=Completed");
            }
            catch (Exception ex)
            {
                UpdateStartupService.LogDiagnostic(
                    "NOTIFICATION CHECK EXCEPTION | Host=" +
                    currentModel +
                    " | Event=" +
                    eventName +
                    " | Type=" +
                    ex.GetType().FullName +
                    " | Message=" +
                    ex.Message +
                    " | StackTrace=" +
                    ex.StackTrace);

                throw;
            }
        }
    }
}