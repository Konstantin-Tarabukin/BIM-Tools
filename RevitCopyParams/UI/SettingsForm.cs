using System.Windows.Forms;

namespace RevitCopyParams
{
    public partial class SettingsForm : Form
    {
        public bool UseSelection { get; private set; }

        public SettingsForm()
        {
            InitializeComponent();

            this.Text = "Настройки";
            this.Width = 300;
            this.Height = 150;
            this.StartPosition = FormStartPosition.CenterScreen;

            CheckBox cb = new CheckBox();
            cb.Text = "Выбирать параметры перед копированием";
            cb.Checked = true;
            cb.AutoSize = true;
            cb.Top = 20;
            cb.Left = 20;

            Button ok = new Button();
            ok.Text = "OK";
            ok.Width = 80;
            ok.Top = 60;
            ok.Left = 180;

            ok.Click += (s, e) =>
            {
                UseSelection = cb.Checked;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.Add(cb);
            this.Controls.Add(ok);
        }
    }
}