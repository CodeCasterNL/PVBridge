using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Configuration.Protection;
using CodeCaster.PVBridge.ConfigurationUI.WinForms.ConfigurationControls;
using CodeCaster.PVBridge.GoodWe;
using CodeCaster.PVBridge.PVOutput;
using CodeCaster.PVBridge.Utils.GitHub;
using Timer = System.Windows.Forms.Timer;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    /// <summary>
    /// TODO: this is a largely uncommented copy-paste mess from two different forms.
    /// </summary>
    public partial class ConfigurationForm : Form
    {
        private const string StopServiceText = "Stop Servi&ce";
        private const string StartServiceText = "Start Servi&ce";

        private int _timerCallers;

        private GoodWeApiConfigurator.GoodWeStatus _goodWeStatus;
        private PVOutputApiConfigurator.PVOutputStatus _pvOutputStatus;
        private BridgeConfiguration? _loadedConfiguration;

        private ServiceController? _windowsService;
        private ServiceControllerStatus _serviceStatus = 0;
        private static readonly Version? OurVersion = Assembly.GetExecutingAssembly().GetName().Version;

        // TODO: make getter in controls
        private bool IsProperlyConfigured =>
            _goodWeStatus == GoodWeApiConfigurator.GoodWeStatus.InverterSelected
            && _pvOutputStatus == PVOutputApiConfigurator.PVOutputStatus.SystemSelected;

        // Disabled: we want to allow saving during setup to maybe upgrade encryption.
        //// TODO: refactor to support different providers
        //private bool HasConfigurationChanged =>
        //    !goodWeApiConfigurator.GetConfiguration().Equals(_loadedConfiguration?.Providers[0])
        //    || !pvOutputApiConfigurator.GetConfiguration().Equals(_loadedConfiguration?.Providers[1]);

        public ConfigurationForm()
        {
            InitializeComponent();

            goodWeApiConfigurator.StatusChanged += GoodWeStatusChanged;
            pvOutputApiConfigurator.StatusChanged += PVOutputStatusChanged;
        }

        /// <summary>
        /// Report to the installer, ignored after.
        /// </summary>
        private void ConfigurationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Let's not block installation when either API or the service is down.
            // Let the UI start again later when we have IPC to prompt the user to finish configuring.

            //Environment.ExitCode = IsProperlyConfigured 
            //    ? 0
            //    // ERROR_INVALID_DATA
            //    : 13;
        }

        private async void ConfigurationForm_Load(object sender, EventArgs e)
        {
            if (OurVersion != null)
            {
                this.Text = $"PVBridve v{OurVersion} - Configuration";
            }

            UpdateServiceStatus();

            if (!Win32.IsElevated)
            {
                Win32.AddShieldToButton(controlServiceButton);
            }

            await LoadConfigurationAsync();

            serviceStatusTextBox.Text = "";

            serviceStatusTimer.Start();

            await CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            var releaseClient = new GitHubReleaseClient(new HttpClient());

            var latestRelease = await releaseClient.GetLatestAsync();

            if (latestRelease == null)
            {
                return;
            }

            var versionString = latestRelease.TagName;

            if (string.IsNullOrWhiteSpace(versionString) || !Version.TryParse(versionString[1..], out var version))
            {
                return;
            }

            if (version > OurVersion)
            {
                const string newVersion = "There's a new version! ";

                InvokeIfRequired(() =>
                {
                    versionLinkLabel.Text = $"{newVersion}Download {versionString} here.";
                    versionLinkLabel.LinkArea = new LinkArea(newVersion.Length, versionLinkLabel.Text.Length - newVersion.Length);
                    versionLinkLabel.Visible = true;
                });
            }
        }

        private void StatusChanged<TEnum>(TEnum newStatus)
            where TEnum : System.Enum
        {
            Debug.WriteLine($"GoodWe: {_goodWeStatus}, PVOutput: {_pvOutputStatus}, new status: {newStatus} ({typeof(TEnum).Name})");

            saveButton.Enabled = IsProperlyConfigured;// && HasConfigurationChanged;
            syncStartdatePicker.Enabled = IsProperlyConfigured;

            fileStatusLabel.Text = IsProperlyConfigured
                    ?
                    // HasConfigurationChanged 
                    //    ?
                        "Configuration valid, saving enabled." 
                    //    : "No configuration changes."
                : "Read device and system info to continue.";
        }

        private void PVOutputStatusChanged(object? sender, PVOutputApiConfigurator.StatusChangedEventArgs e) => StatusChanged(_pvOutputStatus = e.Status);

        private void GoodWeStatusChanged(object? sender, GoodWeApiConfigurator.StatusChangedEventArgs e) => StatusChanged(_goodWeStatus = e.Status);

        private async void SaveButton_Click(object? sender, EventArgs? e)
        {
            saveButton.Enabled = false;

            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {


                saveButton.Enabled = true;
            }

            var saveTimer = new Timer
            {
                Interval = 1000
            };
            
            saveTimer.Start();
            saveTimer.Tick += (_, _) =>
            {
                InvokeIfRequired(() => {;
                    saveButton.Enabled = true;
                });

                saveTimer.Dispose();
            };
        }

        private async Task SaveAsync()
        {
            _loadedConfiguration ??= new BridgeConfiguration
            {
                GrpcAddress = "PVBRIDGE",
                Providers =
                {
                    new PVOutputConfiguration(),
                    new GoodWeInputConfiguration(),
                },
                InputToOutput =
                {
                    new ()
                    {
                        Input = "GoodWe",
                        Outputs =
                        {
                            "PVOutput",
                            #if DEBUG
                            //"Csv",
                            #endif
                        },
                    }
                }
            };

            var showUnrecognizedConfigurationWarning = false;

            if (_loadedConfiguration.InputToOutput.Count != 1)
            {
                if (MessageBox.Show(this, "A valid but unrecognized configuration was detected, saving might overwrite your customizations.\r\n\r\nContinue?", "PVBridge: unknown configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    showUnrecognizedConfigurationWarning = true;
                }
                else
                {
                    return;
                }
            }

            var goodWeConfig = goodWeApiConfigurator.GetConfiguration();
            var pvOutputConfig = pvOutputApiConfigurator.GetConfiguration();

            _loadedConfiguration.Providers[0] = new GoodWeInputConfiguration(goodWeConfig);
            _loadedConfiguration.Providers[1] = new PVOutputConfiguration(pvOutputConfig);

            // Ignored for now, service goes -14 days.
            //_loadedConfiguration.InputToOutput[0].SyncStart = syncStartdatePicker.Value;

            if (_loadedConfiguration.Providers.Count == 2)
            {
#if DEBUG
                _loadedConfiguration.Providers.Add(new PVOutputConfiguration
                {
                    Type = "Csv"
                });
#endif
            }

            await ConfigurationProtector.ProtectAsync(_loadedConfiguration);
            
            var config = new Dictionary<string, BridgeConfiguration>
            {
                { BridgeConfiguration.SectionName, _loadedConfiguration }
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            });

            // This triggers the config reload by the service.
            await File.WriteAllTextAsync(ConfigurationReader.GlobalSettingsFilePath, json);

            // To allow editing again.
            await ConfigurationProtector.UnprotectAsync(_loadedConfiguration);

            fileStatusLabel.Text = "Configuration saved successfully.";

            saveButton.Enabled = IsProperlyConfigured;// && HasConfigurationChanged;

            if (showUnrecognizedConfigurationWarning)
            {
                fileStatusLabel.Text += " Warning: unrecognized configuration detected, we may have overwritten something.";
            }
        }

        private async void ReloadButton_Click(object sender, EventArgs e)
        {
            await LoadConfigurationAsync();
        }

        private async Task LoadConfigurationAsync()
        {
            fileStatusLabel.Text = "Loading configuration...";

            // TODO: warn about changes.
            try
            {
                if (!File.Exists(ConfigurationReader.GlobalSettingsFilePath))
                {
                    fileStatusLabel.Text = "No configuration found. Enter your information to start syncing your systems.";

                    return;
                }

                _loadedConfiguration = await ConfigurationReader.ReadFromSystemAsync();

                if (_loadedConfiguration != null)
                {
                    await ConfigurationProtector.UnprotectAsync(_loadedConfiguration);
                }
            }
            catch (Exception exception)
            {
                //TODO: _logger.LogError(exception, ...)

                fileStatusLabel.Text = "Error parsing configuration file. Either fix it and reload, or enter new information here to overwrite it.";

                _loadedConfiguration = null;

                return;
            }

            var onlyConfig = _loadedConfiguration?.InputToOutput.FirstOrDefault();
            if (onlyConfig == null || _loadedConfiguration!.InputToOutput.Count > 1)
            {
                fileStatusLabel.Text = "Empty configuration file loaded, please enter your credentials.";

                saveButton.Enabled = IsProperlyConfigured;// && HasConfigurationChanged;

                return;
            }

            // TODO: move to validator, Save() also needs this.
            // TODO: support all if()s below

            var unsupportedConfig = false;

            if (onlyConfig.Input.ToUpperInvariant() != "GOODWE")
            {
                fileStatusLabel.Text += " Warning: there's an existing input configuration, but it's not for GoodWe.";

                unsupportedConfig = true;
            }

            if (onlyConfig.Outputs.Count != 1)
            {
                fileStatusLabel.Text += " Warning: there are multiple outputs configured.";

                unsupportedConfig = true;
            }

            if (onlyConfig.Outputs[0].ToUpperInvariant() != "PVOUTPUT")
            {
                fileStatusLabel.Text += " Warning: there's an existing output configuration, but it's not for PVOutput.";

                unsupportedConfig = true;
            }

            if (unsupportedConfig)
            {
                fileStatusLabel.Text += " This is not yet supported in this UI. Saving will overwrite your modifications.";
            }
            else
            {
                fileStatusLabel.Text = "Configuration file loaded.";
            }

            await goodWeApiConfigurator.LoadConfigurationAsync(_loadedConfiguration.Providers[0]);

            await pvOutputApiConfigurator.LoadConfigurationAsync(_loadedConfiguration.Providers[1]);
        }

        private void ServiceStatusTimer_Tick(object? sender, EventArgs? e)
        {
            UpdateServiceStatus();
        }

        private void UpdateServiceStatus()
        {
            // Only if we're first, there can be overlapping calls.
            if (Interlocked.CompareExchange(ref _timerCallers, 1, 0) == 1)
            {
                return;
            }

            try
            {
                UpdateWindowsServiceStatus();
            }
            catch (Exception exception)
            {
                // TODO: log
                _serviceStatus = 0;

                serviceStatusTextBox.Text = $"not installed?\r\n{exception.ToString()}";

                InvokeIfRequired(UpdateServiceControlButton);
            }
            finally
            {
                Interlocked.Exchange(ref _timerCallers, 0);
            }
        }

        private void UpdateWindowsServiceStatus()
        {
            _windowsService ??= new ServiceController("PVBridge");

            _windowsService.Refresh();

            _serviceStatus = _windowsService.Status;

            InvokeIfRequired(() =>
            {
                serviceStatusTextBox.Text = _serviceStatus.ToString();

                UpdateServiceControlButton();
            });
        }

        private void ServiceStatusTextBox_TextChanged(object sender, EventArgs e)
        {
            serviceStatusTextBox.ForeColor = serviceStatusTextBox.Text != "Running"
                ? Color.Red
                : serviceStatusStaticLabel.ForeColor;
        }

        private void ControlServiceButton_Click(object sender, EventArgs e)
        {
            if (!Win32.IsElevated)
            {
                Win32.RestartApplicationElevated();
                return;
            }

            _windowsService ??= new ServiceController("PVBridge");

            _windowsService.Refresh();

            var tag = controlServiceButton.Tag as string;

            switch (tag)
            {
                case "stop":
                    _windowsService.Stop();
                    break;
                case "start":
                    _windowsService.Start();
                    break;
                default:
                    throw new ArgumentException($"Unknown button tag {tag}");
            }

            UpdateServiceStatus();
        }

        private void UpdateServiceControlButton()
        {
            var isRunning = _serviceStatus == ServiceControllerStatus.Running;

            controlServiceButton.Tag = isRunning
                ? "stop"
                : "start";

            controlServiceButton.Text = isRunning
                ? StopServiceText
                : StartServiceText;
        }

        private void InvokeIfRequired(Action action)
        {
            void MethodInvoker()
            {
                action();
            }

            if (InvokeRequired)
            {
                Invoke((MethodInvoker)MethodInvoker);
            }
            else
            {
                MethodInvoker();
            }
        }

        private void VersionLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = "https://codecasternl.github.io/PVBridge/downloads".Replace("&", "^&");

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
    }
}
