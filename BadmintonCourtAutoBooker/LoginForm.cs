using System;
using System.Linq;
using System.Windows.Forms;
using static BadmintonCourtAutoBooker.BookingBot;

namespace BadmintonCourtAutoBooker
{
    public partial class LoginForm : Form
    {
        internal string Username { get; set; }
        internal string Password { get; set; }
        internal SportCenter SportCenter { get; set; }
        internal BookingBot BookingBot { get; set; }


        public LoginForm() => InitializeComponent();

        private void LoginForm_Load(object sender, EventArgs e)
        {
            usernameTextBox.UseSystemPasswordChar = true;
            usernameTextBox.Text = Username;
            passwordTextBox.UseSystemPasswordChar = true;
            passwordTextBox.Text = Password;

            sportCenterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sportCenterComboBox.Items.Clear();
            foreach (SportCenter sportCenter in MainForm.SportCenters)
            {
                sportCenterComboBox.Items.Add(sportCenter.Name);
            }
            if (SportCenter != null)
            {
                sportCenterComboBox.SelectedIndex = sportCenterComboBox.Items.IndexOf(SportCenter.Name);
            }

            usernameTextBox.SetWaterMark("[Username]");
            passwordTextBox.SetWaterMark("[Password]");
            this.KeyPreview = true;
            this.SetAttributes(text: "Login", icon: Properties.Resources.badminton_512x512);
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text) || string.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("Username and Password cannot be empty.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Username = usernameTextBox.Text;
                Password = passwordTextBox.Text;
                SportCenter = MainForm.SportCenters.First(sportcCenter => sportcCenter.Name == sportCenterComboBox.SelectedItem.ToString());

                SetFormControl(false);
                new System.Threading.Thread(() =>
                {
                    BookingBot = new BookingBot(SportCenter.BaseUrl, SportCenter.ModuleName);
                    LoginStatus loginStatus = BookingBot.Login(Username, Password);
                    switch (loginStatus)
                    {
                        case LoginStatus.Success:
                            MessageBox.Show($"Login {SportCenter.Name} successful.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            break;
                        default:
                            MessageBox.Show($"Login {SportCenter.Name} failed ({loginStatus}).", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            SetFormControl(true);
                            break;
                    }
                }).Start();
            }
        }

        private void SetFormControl(bool status = true)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    SetFormControl(status: status);
                }));
            }
            else
            {
                panel1.Enabled = status;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                loginButton.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
