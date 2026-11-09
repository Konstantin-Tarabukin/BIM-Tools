using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using RevitCopyParams.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Interop;

namespace RevitCopyParams.Services
{
    public class JsonJournalNotificationService
    {
        private static readonly TimeSpan StatePersistInterval =
            TimeSpan.FromSeconds(30);

        private readonly IJournalReader _reader;
        private readonly JsonJournalStateService _stateService;

        public JsonJournalNotificationService()
            : this(
                new JsonJournalReader(),
                new JsonJournalStateService())
        {
        }

        public JsonJournalNotificationService(
            IJournalReader reader,
            JsonJournalStateService stateService)
        {
            _reader = reader;
            _stateService = stateService;
        }

        public void CheckNotifications(
            Document document,
            string eventName)
        {
            string journalRoot;
            string projectId;
            string currentModel;

            if (!JsonJournalPathService.TryGetJournalRoot(
                    document,
                    out journalRoot,
                    out projectId,
                    out currentModel))
            {
                return;
            }
            //лог
            UpdateStartupService.LogDiagnostic(
    "JSON CHECK START | Event=" +
    eventName +
    " | Project=" +
    projectId +
    " | Model=" +
    currentModel +
    " | JournalRoot=" +
    journalRoot);
            //лог

            JournalModelState state =
                _stateService.GetModelState(
                    projectId,
                    currentModel);
            //лог
            UpdateStartupService.LogDiagnostic(
    "JSON STATE | Model=" +
    currentModel +
    " | LastScanUtc=" +
    state.LastScanUtc +
    " | Pending=" +
    state.PendingEvents.Count +
    " | Read=" +
    state.ReadEvents.Count);
            //лог

            DateTime scanStartedUtc = DateTime.UtcNow;

            List<string> pendingPaths =
                state.PendingEvents.Values.ToList();

            List<JournalEventFile> files =
                _reader.ReadSince(
                    journalRoot,
                    state.LastScanUtc,
                    pendingPaths);

            List<JournalEventFile> matchingFiles =
                new List<JournalEventFile>();

            foreach (JournalEventFile file in files)
            {
                if (file == null || file.Event == null)

                    continue;

                JournalEvent journalEvent = file.Event;
                //лог

                UpdateStartupService.LogDiagnostic(
    "JSON FILE | Receiver=" +
    currentModel +
    " | EventId=" +
    journalEvent.EventId +
    " | Source=" +
    journalEvent.ModelId +
    " | Project=" +
    journalEvent.ProjectId +
    " | CreatedUtc=" +
    journalEvent.CreatedUtc +
    " | Targets=" +
    string.Join(",", journalEvent.TargetModels ?? new List<string>()));
                //лог

                


                

                if (!string.Equals(
                        journalEvent.ProjectId,
                        projectId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(
                        journalEvent.ModelId,
                        currentModel,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        journalEvent.Author,
                        document.Application.Username,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool isTarget =
                    journalEvent.TargetModels != null &&
                    journalEvent.TargetModels.Any(
                        x => string.Equals(
                            x,
                            currentModel,
                            StringComparison.OrdinalIgnoreCase));

                if (!isTarget)
                    continue;

                _stateService.AddPending(
                    state,
                    journalEvent.EventId,
                    file.RelativePath);

                if (_stateService.IsRead(
                        state,
                        journalEvent.EventId))
                {
                    continue;
                }

                matchingFiles.Add(file);
            }

            state.LastScanUtc = scanStartedUtc;

            if (matchingFiles.Count == 0)
            {
                _stateService.SaveIfDue(
                    StatePersistInterval);

                //лог

                UpdateStartupService.LogDiagnostic(
    "JSON MATCHING | Model=" +
    currentModel +
    " | Count=0");

                //лог
                return;
            }

            List<ChangeRecord> notifications =
                matchingFiles
                    .Select(MapToChangeRecord)
                    .Where(x => x != null)
                    .ToList();

            if (notifications.Count == 0)
            {
                _stateService.Save();
                return;
            }

            _stateService.Save();

            //лог
            UpdateStartupService.LogDiagnostic(
    "JSON NOTIFICATION WINDOW | Model=" +
    currentModel +
    " | Count=" +
    notifications.Count);

            //лог

            NotificationWindow window =
                new NotificationWindow(notifications);

            WindowInteropHelper helper =
                new WindowInteropHelper(window);

            helper.Owner =
                Process.GetCurrentProcess().MainWindowHandle;

            bool? userConfirmed =
                window.ShowDialog();

            if (userConfirmed == true)
            {
                _stateService.MarkAsRead(
                    state,
                    notifications.Select(x => x.Id));

                _stateService.Save();
            }
        }

        public void SaveState()
        {
            _stateService.Save();
        }

        private static ChangeRecord MapToChangeRecord(
            JournalEventFile file)
        {
            if (file == null || file.Event == null)
                return null;

            JournalEvent journalEvent =
                file.Event;

            ChangeRecord record =
                new ChangeRecord();

            record.Id = journalEvent.EventId;
            record.Description = journalEvent.Message;
            record.TargetModels =
                journalEvent.TargetModels == null
                    ? new List<string>()
                    : new List<string>(journalEvent.TargetModels);
            record.CreatedDate =
                journalEvent.CreatedUtc.ToLocalTime();
            record.Author = journalEvent.Author;
            record.SourceModel = journalEvent.ModelId;
            record.IsEdited = journalEvent.IsEdited;
            record.ModifiedDate = journalEvent.ModifiedUtc.HasValue
                ? journalEvent.ModifiedUtc.Value.ToLocalTime()
                : (DateTime?)null;
            record.Status = ChangeStatus.New;

            return record;
        }
    }
}
