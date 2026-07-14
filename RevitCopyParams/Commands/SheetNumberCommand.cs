using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitCopyParams
{
    [Transaction(TransactionMode.Manual)]
    public class SheetNumberCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UI.SheetNumberWindow window = new UI.SheetNumberWindow(commandData.Application);

            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}