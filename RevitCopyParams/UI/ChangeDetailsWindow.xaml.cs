using System.Linq;
using System.Windows;

namespace RevitCopyParams
{



        public partial class ChangeDetailsWindow : Window
        {
            private readonly Autodesk.Revit.DB.Document _document;

            private readonly ChangeRecord _record;

            public bool EditRequested
            {
                get;
                private set;
            }
            public ChangeDetailsWindow(
            Autodesk.Revit.DB.Document document,
            ChangeRecord record)
        {
            InitializeComponent();

            _document = document;

            if (record.Status == ChangeStatus.Mine)
            {
                btnEdit.Visibility =
                    Visibility.Visible;
            }

            tbSource.Text =
                record.SourceModel;

            tbDate.Text =
                record.DisplayDate;

            tbAuthor.Text =
                record.Author;

            tbTargets.Text =
                string.Join(
                    ", ",
                    record.TargetModels);

            tbDescription.Text =
                record.Description;

            NotificationStateService.MarkAsRead(
    document,
    record.Id);
        }

        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        private void Edit_Click(
    object sender,
    RoutedEventArgs e)
        {
            EditRequested = true;

            DialogResult = true;
        }


    }







}