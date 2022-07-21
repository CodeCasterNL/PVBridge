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

        private GoodWeClient? _client;

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

            var plantList = await client.GetPlantListAsync(new CancellationToken());

            if (plantList.Data == null)
            {
                SetStatus(GoodWeStatus.Uninitialized);
                statusLabel.Text = "Could not obtain inverter list, verify your email and password on semsportal.com.";

                return;
            }

            SetStatus(GoodWeStatus.Authorized);

            var firstPlant = plantList.Data.list?.FirstOrDefault();

            if (firstPlant?.inverters?.Any() != true)
            {
                statusLabel.Text = "No inverters found for that account.";

                return;
            }

            plantComboBox.Enabled = true;
            plantComboBox.ValueMember = nameof(AddressWithInverters.id);
            plantComboBox.DisplayMember = nameof(AddressWithInverters.DisplayString);

            _client = client;
            plantComboBox.DataSource = plantList.Data.list;

            plantComboBox.SelectedIndex = 0;

            var plantCount = plantList.Data.list?.Count ?? 0;
            var inverterCount = plantList.Data.list?.Sum(p => p.inverters?.Count) ?? 0;

            if (inverterCount > 0)
            {
                statusLabel.Text = $"Found {plantCount.SIfPlural("plant")} with {inverterCount.SIfPlural("inverter")}";
            }
        }

        private async void PlantComboBox_SelectedValueChanged(object sender, EventArgs e)
        {
            ResetComboBox(inverterComboBox);

            if (plantComboBox.SelectedItem == null)
            {
                _client = null;

                SetStatus(GoodWeStatus.Uninitialized);

                return;
            }

            var plant = plantComboBox.SelectedItem as AddressWithInverters;

            if (plant?.inverters?.Any() != true)
            {
                statusLabel.Text = "No inverters found in selected plant.";

                return;
            }

            if (plant.inverters.Any(p => p.turnon_time == null))
            {
                var deets = await _client!.GetMonitorDetailRaw(plant.id!, new CancellationToken());
                if (deets.Data?.inverter?.Any() != true)
                {
                    statusLabel.Text = "No inverters details found for selected plant.";

                    return;
                }

                foreach (var inverter in plant.inverters)
                {
                    var inverterDetails = deets.Data.inverter.FirstOrDefault(i => i.sn == inverter.sn);

                    if (inverterDetails?.turnon_time != null)
                    {
                        inverter.turnon_time = inverterDetails.turnon_time;
                    }
                }
            }

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

            _client = null;
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
            config.InstallDate = inverter?.turnon_time;

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
