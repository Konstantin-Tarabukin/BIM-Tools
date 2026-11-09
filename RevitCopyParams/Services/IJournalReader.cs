using RevitCopyParams.Models;
using System;
using System.Collections.Generic;

namespace RevitCopyParams.Services
{
    // Интерфейс чтения внешнего журнала (ADR-014, этап A.3).
    public interface IJournalReader
    {
        // Читает события за период, адресованные модели targetModelName.
        //
        // Reader обязан:
        //   * игнорировать .tmp-файлы (A.6);
        //   * валидировать JSON и SchemaVersion (A.9);
        //   * пропускать битые файлы без падения Revit (A.15);
        //   * читать только актуальные каталоги дат (A.5, A.9).
        List<JournalEvent> ReadEvents(
            JournalLocation location,
            DateTime utcFromInclusive,
            string targetModelName);
    }
}
