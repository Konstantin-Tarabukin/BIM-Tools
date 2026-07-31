using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace RevitCopyParams
{
    public partial class ChangeCreateWindow : Window
    {
        private Document _document;

        public string DescriptionText => DescriptionTextBox.Text;

        public List<string> SelectedModels { get; private set; }

        


        public ChangeCreateWindow(Document document)
        {
            InitializeComponent();

            _document = document;

            LoadLinks();
        }

        private void LoadLinks()
        {
            List<string> links =
                RevitLinkService.GetLoadedLinks(_document);


            foreach (string link in links)
            {
                CheckBox checkBox = new CheckBox();

                checkBox.Content = link;

                checkBox.Margin = new Thickness(0, 2, 0, 2);

                LinksPanel.Children.Add(checkBox);
            }
        }


        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            List<string> selectedModels = new List<string>();

            foreach (CheckBox checkBox in LinksPanel.Children)
            {
                if (checkBox.IsChecked == true)
                {
                    selectedModels.Add(checkBox.Content.ToString());
                }
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show(
                    "Введите описание изменения.",
                    "BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedModels.Count == 0)
            {
                MessageBox.Show(
                    "Выберите хотя бы один раздел.",
                    "BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

SelectedModels = selectedModels;

DialogResult = true;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            
        }
    }
}