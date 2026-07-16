using Autodesk.Revit.DB;

namespace RevitCopyParams.Models
{
    public class SheetRow
    {
        public ViewSheet Sheet { get; set; }

        public string CurrentNumber { get; set; }

        public string NewNumber { get; set; }

        public string SheetName { get; set; }

        public bool HasDuplicate { get; set; }

        public bool OrderChanged { get; set; }
    }
}