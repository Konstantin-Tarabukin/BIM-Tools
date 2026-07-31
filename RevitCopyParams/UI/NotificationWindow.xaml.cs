using System.Collections.Generic;
using System.Windows;

namespace RevitCopyParams.UI
{
    public partial class NotificationWindow : Window
    {
        public NotificationWindow(
            List<ChangeRecord> records)
        {
            InitializeComponent();

            lvNotifications.ItemsSource = records;
        }

        private void Ok_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}