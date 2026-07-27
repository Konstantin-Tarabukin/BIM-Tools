using System.Windows;

namespace RevitCopyParams
{
    public partial class ChangeJournalWindow : Window
    {
        public ChangeJournalWindow()
        {
            InitializeComponent();

            foreach (ChangeRecord record in ChangeStorage.GetAll())
            {
                lbChanges.Items.Add(
                    record.CreatedDate.ToString("dd.MM.yyyy HH:mm")
                    + " | "
                    + record.Author
                    + " | "
                    + record.Disciplines
                    + " | "
                    + record.Description);
            }
        }
    }
}