namespace RevitCopyParams.Models
{
    // Ссылка на элемент модели внутри события журнала (ADR-014, этап A.2).
    //
    // Основной стабильный идентификатор — Element.UniqueId.
    // Обычный ElementId не является постоянным идентификатором
    // и хранится только как дополнительная информация.
    public class ChangeElementReference
    {
        // Element.UniqueId — основной идентификатор.
        public string UniqueId { get; set; }

        // Числовой ElementId. В Revit 2023 идентификатор целочисленный;
        // long — задел на будущие версии Revit.
        public long ElementId { get; set; }

        // Модель-источник элемента.
        public string SourceModel { get; set; }

        // Имя категории элемента (например, Walls).
        public string Category { get; set; }

        // Имя элемента.
        public string Name { get; set; }
    }
}
