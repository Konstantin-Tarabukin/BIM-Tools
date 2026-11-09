namespace RevitCopyParams.Models
{
    // Расположение внешнего журнала проекта (ADR-014, этап A.4).
    //
    // Определяется один раз на Revit API-потоке и кэшируется,
    // чтобы background-поток polling не обращался к Revit API.
    public class JournalLocation
    {
        // Полный путь к корню журнала:
        // \\server\share\...\05091-ЭФ25\_BIMToolsJournal
        public string JournalRoot { get; set; }

        // Идентификатор проекта — имя папки проекта.
        public string ProjectId { get; set; }

        // Короткий идентификатор модели — имя папки модели
        // (или имя central-файла, если модель лежит в корне проекта).
        public string ModelId { get; set; }

        // Имя модели — имя central-файла без расширения.
        public string ModelName { get; set; }
    }
}
