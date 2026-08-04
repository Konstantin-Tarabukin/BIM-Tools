using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;

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
                ChangeJournalService.GetJournalForCurrentModel(
                    _uiApp.ActiveUIDocument.Document))
            {
                lvJournal.Items.Add(record);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ChangeCreateWindow createWindow =
                new ChangeCreateWindow(
                    _uiApp.ActiveUIDocument.Document);

            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();

            if (result != true)
                return;

            ChangeRecord record = new ChangeRecord
            {


                Description = createWindow.DescriptionText,


                TargetModels = createWindow.SelectedModels,


                Author = _uiApp.Application.Username,


                SourceModel =
    RevitLinkService.GetModelName(
        _uiApp.ActiveUIDocument.Document)
            };



            ChangeExtensibleStorageService.AddRecord(
                _uiApp.ActiveUIDocument.Document,
                record);

            RefreshJournal();
        }

        private void OpenSelectedRecord()
        {
            ChangeRecord record =
                lvJournal.SelectedItem as ChangeRecord;

            if (record == null)
                return;

            ChangeDetailsWindow window =
                new ChangeDetailsWindow(
                    _uiApp.ActiveUIDocument.Document,
                    record);

            window.Owner = this;

            window.ShowDialog();

            RefreshJournal();
        }

        private void lvJournal_MouseDoubleClick(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelectedRecord();
        }


        private void lvJournal_KeyDown(
    object sender,
    System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;

            OpenSelectedRecord();

            e.Handled = true;
        }
    }
}