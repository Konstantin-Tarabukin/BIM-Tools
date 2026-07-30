using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System.Windows.Interop;

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
            ChangeJournalWindow window =
                new ChangeJournalWindow(
                    commandData.Application);

            WindowInteropHelper helper =
                new WindowInteropHelper(window);

            helper.Owner =
                commandData.Application.MainWindowHandle;

            window.ShowDialog();

           

            return Result.Succeeded;
        }
    }
}