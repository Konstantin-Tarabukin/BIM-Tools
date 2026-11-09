using Autodesk.Revit.DB;
using RevitCopyParams.Models;
using System;
using System.Collections.Generic;

namespace RevitCopyParams.Services
{
    public static class JournalEventFactory
    {
        public static JournalEvent FromChangeRecord(
            Document document,
            string projectId,
            ChangeRecord record)
        {
            string currentModel =
                RevitLinkService.GetModelName(document);

            DateTime createdUtc =
                ToUtc(record.CreatedDate);

            DateTime? modifiedUtc = null;

            if (record.ModifiedDate.HasValue)
            {
                modifiedUtc =
                    ToUtc(record.ModifiedDate.Value);
            }

            return new JournalEvent
            {
                SchemaVersion = 1,
                EventId = record.Id,
                ProjectId = projectId,
                ModelId = string.IsNullOrWhiteSpace(record.SourceModel)
                    ? currentModel
                    : record.SourceModel,
                ModelName = currentModel,
                Author = record.Author,
                CreatedUtc = createdUtc,
                EventType = record.IsEdited
                    ? "ChangeUpdated"
                    : "ChangeCreated",
                Message = record.Description,
                TargetModels = record.TargetModels == null
                    ? new List<string>()
                    : new List<string>(record.TargetModels),
                IsEdited = record.IsEdited,
                ModifiedUtc = modifiedUtc,
                Elements = new List<ChangeElementReference>()
            };
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            return value.ToUniversalTime();
        }
    }
}
