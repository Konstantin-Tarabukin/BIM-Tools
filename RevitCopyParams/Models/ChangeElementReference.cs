using System;

namespace RevitCopyParams.Models
{
    public class ChangeElementReference
    {
        public string UniqueId { get; set; }
        public int ElementId { get; set; }
        public string SourceModel { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
    }
}
