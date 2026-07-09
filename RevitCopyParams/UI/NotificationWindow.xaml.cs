using Autodesk.Revit.DB;
using System.Windows;

namespace RevitCopyParams.UI
{
    public partial class NotificationWindow : Window
    {
        private Document document;
        private string eventName;

        public NotificationWindow(Document document, string eventName)
        {
            InitializeComponent();

            this.document = document;
            this.eventName = eventName;
            EventText.Text = "Событие: " + eventName;
            ProjectText.Text = "Проект: " + document.Title;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}