using System;
using System.Collections.Generic;

namespace RevitCopyParams.Models
{
    public class JournalClientState
    {
        public Dictionary<string, JournalModelState> Models { get; set; }

        public JournalClientState()
        {
            Models = new Dictionary<string, JournalModelState>(
                StringComparer.OrdinalIgnoreCase);
        }
    }

    public class JournalModelState
    {
        public DateTime LastScanUtc { get; set; }

        public Dictionary<Guid, DateTime> ReadEvents { get; set; }

        public Dictionary<Guid, string> PendingEvents { get; set; }

        public JournalModelState()
        {
            LastScanUtc = DateTime.UtcNow.AddDays(-1);
            ReadEvents = new Dictionary<Guid, DateTime>();
            PendingEvents = new Dictionary<Guid, string>();
        }
    }
}
