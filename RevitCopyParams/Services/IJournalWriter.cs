using RevitCopyParams.Models;

namespace RevitCopyParams.Services
{
    public interface IJournalWriter
    {
        void Enqueue(
            string journalRoot,
            JournalEvent journalEvent);
    }
}
