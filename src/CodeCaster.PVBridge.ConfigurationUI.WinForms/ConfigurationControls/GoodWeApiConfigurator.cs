using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeCaster.GoodWe;
using CodeCaster.GoodWe.Json;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.GoodWe;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls
{
    public partial class GoodWeApiConfigurator : UserControl
    {
        #region Status
        internal enum GoodWeStatus
        {
            Uninitialized,
            Authorized,
            InverterSelected
        }

        internal class StatusChangedEventArgs : EventArgs
        {
            public GoodWeStatus Status { get; }

            public StatusChangedEventArgs(GoodWeStatus status)
            {
                Status = status;
            }
        }

        internal EventHandler<StatusChangedEventArgs> StatusChanged = delegate { };
        
        private CaseInsensitiveDictionary<string?>? _optionsBackup;

        private void SetStatus(GoodWeStatus status)
        {
            StatusChanged(this, new StatusChangedEventArgs(status));
        }
        #endregion Status

        // TODO: inject
        private readonly ILogger<GoodWeClient> _logger = new NullLogger<GoodWeClient>();

        public GoodWeApiConfigurator()
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
            SetStatus(GoodWeStatus.Uninitialized);
            statusLabel.Text = "Logging in...";

            ResetComboBox(plantComboBox);
            ResetComboBox(inverterComboBox);

            if (emailTextBox.Text.IndexOf("@", StringComparison.Ordinal) == -1)
            {
                statusLabel.Text = "Please enter a valid email address.";

                return;
            }

            if (passwordTextBox.Text.Length < 1)
            {
                statusLabel.Text = "Please enter your password.";

                return;
            }

            var accountConfig = new AccountConfiguration(emailTextBox.Text, passwordTextBox.Text);

            var client = new GoodWeClient(_logger, null, accountConfig);

            var stats = await client.GetPlantListAsync(new CancellationToken());

            if (stats.Data == null)
            {
                SetStatus(GoodWeStatus.Uninitialized);
                statusLabel.Text = "Could not obtain inverter list, verify your email and password on semsportal.com.";

                return;
            }

            SetStatus(GoodWeStatus.Authorized);

            var firstPlant = stats.Data.list?.FirstOrDefault();

            if (firstPlant?.inverters?.Any() != true)
            {
                statusLabel.Text = "No inverters found for that account.";

                return;
            }

            var plantCount = stats.Data.list?.Count ?? 0;
            var inverterCount = stats.Data.list?.Sum(p => p.inverters?.Count);

            statusLabel.Text = $"Found {plantCount} plant{(plantCount == 1 ? "" : "s")} with {inverterCount} inverter{(inverterCount == 1 ? "" : "s")}";

            plantComboBox.Enabled = true;
            plantComboBox.ValueMember = nameof(AddressWithInverters.id);
            plantComboBox.DisplayMember = nameof(AddressWithInverters.DisplayString);
            plantComboBox.DataSource = stats.Data.list;

            plantComboBox.SelectedIndex = 0;
        }

        private void PlantComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            ResetComboBox(inverterComboBox);

            if (plantComboBox.SelectedItem == null)
            {
                SetStatus(GoodWeStatus.Uninitialized);

                return;
            }

            var plant = plantComboBox.SelectedItem as AddressWithInverters;

            if (plant?.inverters?.Any() != true)
            {
                statusLabel.Text = "No inverters found in selected plant.";

                return;
            }

            statusLabel.Text = "";

            inverterComboBox.Enabled = true;
            inverterComboBox.ValueMember = nameof(Inverter.sn);
            inverterComboBox.DisplayMember = nameof(Inverter.sn);
            inverterComboBox.DataSource = plant.inverters;

            inverterComboBox.SelectedIndex = 0;
        }

        private void InverterComboBox_SelectedValueChanged(object sender, EventArgs e) =>
            SetStatus(inverterComboBox.SelectedItem != null
                ? GoodWeStatus.InverterSelected
                : GoodWeStatus.Uninitialized);

        private void Credentials_TextChanged(object sender, EventArgs e)
        {
            ResetComboBox(plantComboBox);
            ResetComboBox(inverterComboBox);

            readDeviceInfoButton.Enabled = emailTextBox.TextLength > 0 && passwordTextBox.TextLength > 0;
        }

        private void SemsPortalLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            semsPortalLinkLabel.LinkVisited = true;

            var psi = new ProcessStartInfo("https://semsportal.com/")
            {
                UseShellExecute = true
            };

            Process.Start(psi);
        }

        public async Task LoadConfigurationAsync(DataProviderConfiguration config)
        {
            emailTextBox.Text = config.Account;
            passwordTextBox.Text = config.Key;

            _optionsBackup = new CaseInsensitiveDictionary<string?>(config.Options);
            
            await ReadDeviceInfoAsync();
        }

        public DataProviderConfiguration GetConfiguration()
        {
            var plant = plantComboBox.SelectedItem as AddressWithInverters;
            var inverter = inverterComboBox.SelectedItem as Inverter;

            var config = new GoodWeInputConfiguration
            {
                // Set Options before everything else.
                Options = _optionsBackup ?? new(),
                
                Account = emailTextBox.Text,
                Description = plant?.Description,
                Key = passwordTextBox.Text,
                IsProtected = false,
            };

            // Set options properties separately.
            config.PlantId = plant?.id;
            config.InverterSerialNumber = inverter?.sn;
            config.InstallDate = inverter?.GetTurnonTime();

            return config;
        }

        private void ResetComboBox(ComboBox comboBox)
        {
            comboBox.SelectedItem = null;
            comboBox.DataSource = null;
            comboBox.Enabled = false;
        
            SetStatus(GoodWeStatus.Uninitialized);
        }
    }
}
