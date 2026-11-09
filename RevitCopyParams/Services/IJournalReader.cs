using RevitCopyParams.Models;
using System;
using System.Collections.Generic;

namespace RevitCopyParams.Services
{
    public interface IJournalReader
    {
        List<JournalEventFile> ReadSince(
            string journalRoot,
            DateTime sinceUtc,
            IEnumerable<string> pendingRelativePaths);
    }
}
