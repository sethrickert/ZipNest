using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ZipNest
{
    internal sealed class PasswordDialog : Form
    {
        private readonly TextBox _password;

        private PasswordDialog(string archivePath, int attempt)
        {
            Text = "Password required — ZipNest";
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(430, 166);
            BackColor = Color.White;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var heading = new Label
            {
                AutoSize = false,
                Text = attempt > 1 ? "That password did not work. Try again." : "This archive is password protected.",
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(24, 20),
                Size = new Size(382, 22)
            };
            var name = new Label
            {
                AutoEllipsis = true,
                Text = Path.GetFileName(archivePath),
                Location = new Point(24, 47),
                Size = new Size(382, 20),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            _password = new TextBox
            {
                Location = new Point(24, 76),
                Size = new Size(382, 25),
                UseSystemPasswordChar = true
            };
            var ok = new Button
            {
                Text = "Extract",
                DialogResult = DialogResult.OK,
                Location = new Point(234, 119),
                Size = new Size(82, 28)
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(324, 119),
                Size = new Size(82, 28)
            };
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { heading, name, _password, ok, cancel });
        }

        public static string Request(string archivePath, int attempt)
        {
            using (var dialog = new PasswordDialog(archivePath, attempt))
                return dialog.ShowDialog() == DialogResult.OK ? dialog._password.Text : null;
        }
    }

    internal sealed class DialogPasswordProvider : IPasswordProvider
    {
        public string RequestPassword(string archivePath, int attempt)
        {
            return PasswordDialog.Request(archivePath, attempt);
        }
    }
}
