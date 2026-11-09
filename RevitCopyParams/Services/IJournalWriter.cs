using RevitCopyParams.Models;

namespace RevitCopyParams.Services
{
    // Интерфейс записи во внешний журнал (ADR-014, этап A.3).
    //
    // Абстракция backend: текущая реализация — JSON-файлы,
    // в будущем возможна замена на HttpJournalWriter (Journal Service)
    // без изменения модели JournalEvent и логики уведомлений.
    public interface IJournalWriter
    {
        // Асинхронная fire-and-forget запись (A.7):
        // файл I/O выполняется отдельно от Revit API-потока,
        // в Revit-контексте формируется только DTO.
        void WriteAsync(
            JournalEvent journalEvent,
            JournalLocation location);

        // Синхронная best-effort запись.
        // Никогда не бросает исключений: ошибки пишутся в лог.
        bool TryWrite(
            JournalEvent journalEvent,
            JournalLocation location);
    }
}
