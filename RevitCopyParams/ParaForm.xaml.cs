using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitCopyParams
{
    public partial class ParamForm : Window
    {
        public List<string> SelectedParams { get; private set; }

        public ParamForm(List<ParamItem> items)
        {
            InitializeComponent();
            grid.ItemsSource = items;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedParams = ((List<ParamItem>)grid.ItemsSource)
                .Where(x => x.IsChecked)
                .Select(x => x.Name)
                .ToList();

            DialogResult = true;
            Close();
        }

        private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as System.Windows.Controls.CheckBox;

            if (cb == null) return;

            bool isChecked = cb.IsChecked == true;

            foreach (var item in (List<ParamItem>)grid.ItemsSource)
            {
                item.IsChecked = isChecked;
            }

            grid.Items.Refresh();
        }
    }

    public class ParamItem
    {
        public bool IsChecked { get; set; }
        public string Name { get; set; }
        public string HostValue { get; set; }
        public string InsValue { get; set; }
    }
}