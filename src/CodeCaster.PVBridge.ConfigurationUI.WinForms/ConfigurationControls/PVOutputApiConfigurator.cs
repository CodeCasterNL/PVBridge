using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.PVOutput;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls
{
    public partial class PVOutputApiConfigurator : UserControl
    {
        #region Status
        internal enum PVOutputStatus
        {
            Uninitialized,
            Authorized,
            SystemSelected,
        }

        internal class StatusChangedEventArgs : EventArgs
        {
            public PVOutputStatus Status { get; }

            public StatusChangedEventArgs(PVOutputStatus status)
            {
                Status = status;
            }
        }

        internal EventHandler<StatusChangedEventArgs> StatusChanged = delegate { };

        private void SetStatus(PVOutputStatus status)
        {
            StatusChanged(this, new StatusChangedEventArgs(status));
        }
        #endregion Status

        // TODO: inject
        private readonly ILogger<PVOutputApiClient> _logger = new NullLogger<PVOutputApiClient>();

        private string? _accountBackup;
        private CaseInsensitiveDictionary<string?>? _optionsBackup;

        public PVOutputApiConfigurator()
        {
            InitializeComponent();

            statusLabel.Text = "";
        }

        private async void ReadDeviceInfoButton_Click(object sender, EventArgs e)
        {
            await ReadDeviceInfoAsync();
        }

        private async Task ReadDeviceInfoAsync()
        {

            ResetComboBox(systemComboBox);

            if (apiKeyTextBox.Text.Length < 1)
            {
                statusLabel.Text = "Please enter your API key (found in your account page).";

                return;
            }

            if (systemIdTextBox.Text.Length < 1 || !int.TryParse(systemIdTextBox.Text, out _))
            {
                statusLabel.Text = "Please enter your System ID (found in your account page).";

                return;
            }

            var client = new PVOutputApiClient(_logger, null, null!);

            var config = new PVOutputConfiguration
            {
                Key = apiKeyTextBox.Text,
                SystemId = systemIdTextBox.Text,
                IsProtected = false,
            };

            statusLabel.Text = "Logging in...";

            var system = await client.GetSystemAsync(config, new CancellationToken());

            if (system == null)
            {
                statusLabel.Text = "Could not obtain system info, verify your API key and system ID (found in your account page).";

                return;
            }

            SetStatus(PVOutputStatus.Authorized);

            statusLabel.Text = "";

            systemComboBox.Enabled = true;
            systemComboBox.ValueMember = nameof(system.Id);
            systemComboBox.DisplayMember = nameof(system.DisplayString);
            systemComboBox.DataSource = new[] { system };
            systemComboBox.SelectedIndex = 0;
        }

        private void SystemComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            SetStatus(systemComboBox.SelectedItem != null
                ? PVOutputStatus.SystemSelected
                : PVOutputStatus.Uninitialized);
        }

        private void Credentials_TextChanged(object sender, EventArgs e)
        {
            ResetComboBox(systemComboBox);

            readSystemInfoButton.Enabled = apiKeyTextBox.TextLength > 0;
        }

        private void PVOutputLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pvOutputLinkLabel.LinkVisited = true;

            var psi = new ProcessStartInfo("https://pvoutput.org/account.jsp")
            {
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        public async Task LoadConfigurationAsync(DataProviderConfiguration config)
        {
            var pvOutputConfig = new PVOutputConfiguration(config);

            _accountBackup = pvOutputConfig.Account;

            _optionsBackup = new CaseInsensitiveDictionary<string?>(pvOutputConfig.Options);

            apiKeyTextBox.Text = pvOutputConfig.Key;
            systemIdTextBox.Text = pvOutputConfig.SystemId;

            await ReadDeviceInfoAsync();
        }

        public DataProviderConfiguration GetConfiguration()
        {
            var system = systemComboBox.SelectedItem as PVOutputSystem;

            var config = new PVOutputConfiguration
            {
                Account = _accountBackup,
                Description = system?.Description,
                Key = apiKeyTextBox.Text,
                Options = _optionsBackup ?? new(),
                IsProtected = false,
            };

            config.SystemId = system?.Id.ToString();

            return config;
        }

        private void ResetComboBox(ComboBox comboBox)
        {
            comboBox.SelectedItem = null;
            comboBox.DataSource = null;
            comboBox.Enabled = false;

            SetStatus(PVOutputStatus.Uninitialized);
        }
    }
}
