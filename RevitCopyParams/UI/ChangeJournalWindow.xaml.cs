using System;
using System.Windows;
using Autodesk.Revit.UI;

namespace RevitCopyParams
{
    public partial class ChangeJournalWindow : Window
    {
        public ChangeJournalWindow(
            UIApplication uiApp)
        {
            InitializeComponent();

            _uiApp = uiApp;

            RefreshJournal();
        }
        private readonly UIApplication _uiApp;
        private void RefreshJournal()
        {
            lvJournal.Items.Clear();

            foreach (ChangeRecord record in
                ChangeExtensibleStorageService.LoadJournal(
                    _uiApp.ActiveUIDocument.Document))
            {
                lvJournal.Items.Add(record);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ChangeCreateWindow createWindow = new ChangeCreateWindow();

            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();

            if (result != true)
                return;

            ChangeRecord record = new ChangeRecord
            {


                Description = createWindow.DescriptionText,

                Disciplines = createWindow.SelectedDisciplines,



                Author = _uiApp.Application.Username, 


                SourceModel = _uiApp.ActiveUIDocument.Document.Title
            };

            ChangeExtensibleStorageService.AddRecord(
                _uiApp.ActiveUIDocument.Document,
                record);

            RefreshJournal();
        }
    }
}