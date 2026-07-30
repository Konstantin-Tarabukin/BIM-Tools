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

            foreach (ChangeRecord record in ChangeStorage.GetAll())
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
                Id = Guid.NewGuid(),

                Description = createWindow.DescriptionText,

                Disciplines = createWindow.SelectedDisciplines,

                CreatedDate = DateTime.Now,

                Author = _uiApp.Application.Username
            };

            ChangeStorage.Add(record);

            RefreshJournal();
        }
    }
}