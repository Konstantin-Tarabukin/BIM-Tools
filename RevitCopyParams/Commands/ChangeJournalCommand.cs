using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
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
            ChangeCreateWindow window = new ChangeCreateWindow();

            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner = commandData.Application.MainWindowHandle;


            bool? dialogResult = window.ShowDialog();


            if (dialogResult == true)
            {
                ChangeRecord record = new ChangeRecord();

                record.Description = window.DescriptionText;

                record.Disciplines = window.SelectedDisciplines;

                record.CreatedDate = DateTime.Now;

                record.Author = Environment.UserName;


                ChangeStorage.Add(record);

                ChangeJournalWindow journalWindow = new ChangeJournalWindow();

                WindowInteropHelper helper2 = new WindowInteropHelper(journalWindow);
                helper2.Owner = commandData.Application.MainWindowHandle;

                journalWindow.ShowDialog();
            }


            return Result.Succeeded;
        }
    }
}