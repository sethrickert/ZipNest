using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ZipNest
{
    internal sealed class SettingsForm : Form
    {
        private readonly SettingsStore _store;
        private readonly AppSettings _settings;
        private readonly CheckBox _explorerIntegration;
        private readonly RadioButton _nest;
        private readonly RadioButton _organize;

        public SettingsForm(SettingsStore store, AppSettings settings)
        {
            _store = store;
            _settings = settings;
            Text = "ZipNest";
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 430);
            BackColor = Color.FromArgb(248, 249, 251);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var title = new Label
            {
                Text = "ZipNest",
                Font = new Font("Segoe UI Semibold", 21F),
                Location = new Point(30, 24),
                AutoSize = true,
                ForeColor = Color.FromArgb(29, 38, 54)
            };
            var subtitle = new Label
            {
                Text = "Archives in. Tidy folders out.",
                Location = new Point(33, 67),
                AutoSize = true,
                ForeColor = Color.FromArgb(92, 100, 114)
            };
            var panel = new Panel
            {
                Location = new Point(28, 105),
                Size = new Size(484, 240),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var integrationHeading = Heading("Explorer integration", 22, 20);
            _explorerIntegration = new CheckBox
            {
                Text = "Show Extract & Nest and Extract & Organize when I right-click archives",
                Location = new Point(22, 52),
                Size = new Size(438, 38),
                Checked = RegistryIntegration.IsExplorerIntegrationInstalled()
            };
            var workflowHeading = Heading("Double-click behavior", 22, 107);
            _nest = new RadioButton
            {
                Text = "Extract & Nest  — always use a same-name folder",
                Location = new Point(22, 138),
                Size = new Size(420, 25),
                Checked = settings.DefaultMode == ExtractionMode.Nest
            };
            _organize = new RadioButton
            {
                Text = "Extract & Organize  — remove a redundant same-name wrapper folder",
                Location = new Point(22, 170),
                Size = new Size(440, 42),
                Checked = settings.DefaultMode == ExtractionMode.Organize
            };
            panel.Controls.AddRange(new Control[] { integrationHeading, _explorerIntegration, workflowHeading, _nest, _organize });

            var defaults = new Button
            {
                Text = "Windows default apps…",
                Location = new Point(28, 366),
                Size = new Size(165, 34)
            };
            defaults.Click += (sender, args) => RegistryIntegration.OpenDefaultApps();
            var save = new Button
            {
                Text = "Save",
                Location = new Point(422, 366),
                Size = new Size(90, 34),
                BackColor = Color.FromArgb(51, 103, 209),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += SaveClick;
            Controls.AddRange(new Control[] { title, subtitle, panel, defaults, save });
            AcceptButton = save;
        }

        private static Label Heading(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 10F),
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(29, 38, 54)
            };
        }

        private void SaveClick(object sender, EventArgs eventArgs)
        {
            try
            {
                _settings.DefaultMode = _organize.Checked ? ExtractionMode.Organize : ExtractionMode.Nest;
                _store.Save(_settings);
                RegistryIntegration.RegisterApplication(Application.ExecutablePath);
                if (_explorerIntegration.Checked)
                    RegistryIntegration.InstallExplorerIntegration(Application.ExecutablePath);
                else
                    RegistryIntegration.RemoveExplorerIntegration();
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "ZipNest could not save this setting", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
