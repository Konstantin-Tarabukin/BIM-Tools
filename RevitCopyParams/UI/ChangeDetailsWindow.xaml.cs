using System.Linq;
using System.Windows;

namespace RevitCopyParams
{
    public partial class ChangeDetailsWindow : Window
    {
        public ChangeDetailsWindow(
            Autodesk.Revit.DB.Document document,
            ChangeRecord record)
        {
            InitializeComponent();

            tbSource.Text =
                record.SourceModel;

            tbDate.Text =
                record.CreatedDate.ToString(
                    "dd.MM.yyyy HH:mm");

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
    }
}