using System.Collections.Generic;
using System.Windows;

namespace RevitCopyParams
{
    public partial class ChangeCreateWindow : Window
    {
        public string DescriptionText => DescriptionTextBox.Text;

        public string SelectedDisciplines { get; private set; }

        public ChangeCreateWindow()
        {
            InitializeComponent();
        }


        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            List<string> disciplines = new List<string>();

            if (cbOV.IsChecked == true)
                disciplines.Add("ОВ");

            if (cbVK.IsChecked == true)
                disciplines.Add("ВК");

            if (cbEOM.IsChecked == true)
                disciplines.Add("ЭОМ");

            if (cbAR.IsChecked == true)
                disciplines.Add("АР");

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show(
                    "Введите описание изменения.",
                    "BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (disciplines.Count == 0)
            {
                MessageBox.Show(
                    "Выберите хотя бы один раздел.",
                    "BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            SelectedDisciplines = string.Join(", ", disciplines);

            DialogResult = true;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            
        }
    }
}