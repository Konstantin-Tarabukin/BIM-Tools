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



        protected override void OnClosing(
    System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult != true)
            {
                DialogResult = false;
            }

            base.OnClosing(e);
        }
    }
}