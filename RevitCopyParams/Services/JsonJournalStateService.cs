using Newtonsoft.Json;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.IO;
using System.Text;

namespace RevitCopyParams.Services
{
    public class JsonJournalStateService
    {
        private readonly object _syncRoot = new object();
        private JournalClientState _state;
        private DateTime _lastPersistUtc = DateTime.MinValue;

        public JsonJournalStateService()
        {
            _state = Load();
        }

        public JournalModelState GetModelState(
            string projectId,
            string modelId)
        {
            string key = CreateKey(
                projectId,
                modelId);

            lock (_syncRoot)
            {
                JournalModelState modelState;

                if (!_state.Models.TryGetValue(
                        key,
                        out modelState) ||
                    modelState == null)
                {
                    modelState = new JournalModelState();
                    _state.Models[key] = modelState;
                }

                if (modelState.ReadEvents == null)
                    modelState.ReadEvents =
                        new System.Collections.Generic.Dictionary<Guid, DateTime>();

                if (modelState.PendingEvents == null)
                    modelState.PendingEvents =
                        new System.Collections.Generic.Dictionary<Guid, string>();

                return modelState;
            }
        }

        public bool IsRead(
            JournalModelState state,
            Guid eventId)
        {
            return state != null &&
                state.ReadEvents != null &&
                state.ReadEvents.ContainsKey(eventId);
        }

        public void AddPending(
            JournalModelState state,
            Guid eventId,
            string relativePath)
        {
            if (state == null || eventId == Guid.Empty)
                return;

            lock (_syncRoot)
            {
                state.PendingEvents[eventId] = relativePath;
            }
        }

        public void MarkAsRead(
            JournalModelState state,
            System.Collections.Generic.IEnumerable<Guid> eventIds)
        {
            if (state == null || eventIds == null)
                return;

            lock (_syncRoot)
            {
                foreach (Guid eventId in eventIds)
                {
                    if (eventId == Guid.Empty)
                        continue;

                    state.ReadEvents[eventId] =
                        DateTime.UtcNow;

                    state.PendingEvents.Remove(eventId);
                }
            }
        }

        public void Save()
        {
            lock (_syncRoot)
            {
                SaveInternal();
            }
        }

        public void SaveIfDue(
            TimeSpan interval)
        {
            if ((DateTime.UtcNow - _lastPersistUtc) < interval)
                return;

            Save();
        }

        private static string CreateKey(
            string projectId,
            string modelId)
        {
            return (projectId ?? string.Empty) +
                "|" +
                (modelId ?? string.Empty);
        }

        private static JournalClientState Load()
        {
            try
            {
                BIMToolsPaths.EnsureDirectories();

                if (!File.Exists(
                        BIMToolsPaths.JournalStatePath))
                {
                    return new JournalClientState();
                }

                string json = File.ReadAllText(
                    BIMToolsPaths.JournalStatePath,
                    Encoding.UTF8);

                JournalClientState state =
                    JsonConvert.DeserializeObject<JournalClientState>(json);

                if (state == null)
                    return new JournalClientState();

                if (state.Models == null)
                    state.Models =
                        new System.Collections.Generic.Dictionary<string, JournalModelState>(
                            StringComparer.OrdinalIgnoreCase);

                return state;
            }
            catch
            {
                return new JournalClientState();
            }
        }

        private void SaveInternal()
        {
            try
            {
                BIMToolsPaths.EnsureDirectories();

                string json = JsonConvert.SerializeObject(
                    _state,
                    Formatting.Indented);

                string targetPath =
                    BIMToolsPaths.JournalStatePath;

                string tempPath =
                    targetPath + ".tmp";

                File.WriteAllText(
                    tempPath,
                    json,
                    new UTF8Encoding(false));

                if (File.Exists(targetPath))
                {
                    try
                    {
                        File.Replace(
                            tempPath,
                            targetPath,
                            null,
                            true);
                    }
                    catch
                    {
                        File.Delete(targetPath);
                        File.Move(tempPath, targetPath);
                    }
                }
                else
                {
                    File.Move(tempPath, targetPath);
                }

                _lastPersistUtc = DateTime.UtcNow;
            }
            catch
            {
            }
        }
    }
}
