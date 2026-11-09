using System;
using System.Collections.Generic;

namespace RevitCopyParams.Models
{
    public class JournalEvent
    {
        public int SchemaVersion { get; set; }
        public Guid EventId { get; set; }
        public string ProjectId { get; set; }
        public string ModelId { get; set; }
        public string ModelName { get; set; }
        public string Author { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public List<string> TargetModels { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? ModifiedUtc { get; set; }
        public List<ChangeElementReference> Elements { get; set; }

        public JournalEvent()
        {
            SchemaVersion = 1;
            TargetModels = new List<string>();
            Elements = new List<ChangeElementReference>();
        }
    }

    public class JournalEventFile
    {
        public JournalEvent Event { get; set; }
        public string RelativePath { get; set; }

        public JournalEventFile()
        {
        }

        public JournalEventFile(
            JournalEvent journalEvent,
            string relativePath)
        {
            Event = journalEvent;
            RelativePath = relativePath;
        }
    }
}
