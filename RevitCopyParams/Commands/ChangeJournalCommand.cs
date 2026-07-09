using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitCopyParams
{
    [Transaction(TransactionMode.Manual)]
    public class ChangeJournalCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            TaskDialog.Show(
                "BIM Tools",
                "Здесь позже откроется окно создания изменения.");

            return Result.Succeeded;
        }
    }
}