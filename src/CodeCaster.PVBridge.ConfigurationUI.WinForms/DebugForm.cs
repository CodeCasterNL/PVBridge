using CodeCaster.PVBridge.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GrpcDotNetNamedPipes;
using CodeCaster.PVBridge.ConfigurationUI.WinForms.Grpc;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    public partial class DebugForm : Form
    {
        private int _timerCallers;
        private readonly ServiceController _windowsService;
        private readonly GrpcClientFactory _grpcClientFactory;

        private NamedPipeChannel? _channel;
        private AsyncServerStreamingCall<Snapshot>? _snapshotStream;
        private AsyncServerStreamingCall<Summary>? _summariesStream;
        private Task? _snapshotTask;
        private Task? _summariesTask;

        private void MainForm_Load(object sender, EventArgs e)
        {
            serviceStatusTimer.Start();
        }

        private void ServiceStatusTimer_Tick(object sender, EventArgs e) => UpdateServiceStatus();

        public DebugForm()
        {
            InitializeComponent();
            if (!Win32.IsElevated)
            {
                Win32.AddShieldToButton(controlServiceButton);
            }

            _windowsService = new ServiceController("PVBridge");
            _grpcClientFactory = new GrpcClientFactory();
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
                SubscribeGrpc();
            }
            catch (Exception exception)
            {
                // TODO: statuslabel, log
            }
            finally
            {
                Interlocked.Exchange(ref _timerCallers, 0);
            }
        }

        // Each subscription hangs forever, so start a thread for each. Can't they multiplex?
        private void SubscribeGrpc()
        {
            if (_snapshotTask == null || _snapshotTask.IsCompleted || _snapshotTask.IsCanceled)
            {
                _snapshotStream?.Dispose();
                _snapshotStream = null;

                _snapshotTask = Task.Run(async () => await SubscribeSnapshotsAsync());
            }

            if (_summariesTask == null || _summariesTask.IsCompleted || _summariesTask.IsCanceled)
            {
                _summariesStream?.Dispose();
                _summariesStream = null;

                _summariesTask = Task.Run(async () => await SubscribeSummariesAsync());
            }
        }

        private void UpdateWindowsServiceStatus()
        {
            try
            {
                _windowsService.Refresh();
                
                var isRunning = _windowsService.Status == ServiceControllerStatus.Running;
                
                isServiceRunningLabel.Text = isRunning
                    ? "yes"
                    : "no";

                controlServiceButton.Tag = isRunning
                    ? "stop"
                    : "start";

                controlServiceButton.Text = isRunning
                    ? "&Stop"
                    : "&Start";

                controlServiceButton.Enabled = true;
            }
            catch (Exception ex)
            {
                isServiceRunningLabel.Text = $"unknown ({ex.Message})";
            }
        }

        private void ControlServiceButton_Click(object sender, EventArgs e)
        {
            if (!Win32.IsElevated)
            {
                Win32.RestartApplicationElevated();
                return;
            }

            // Service status poll will re-enable the button.
            controlServiceButton.Enabled = false;

            var tag = controlServiceButton.Tag as string;
            if (tag == "stop")
            {
                _windowsService.Stop();
            }
            else if (tag == "start")
            {
                _windowsService.Start();
            }
        }

        private async Task SubscribeSnapshotsAsync()
        {
            // TODO: reconnect after error?
            if (_snapshotStream != null)
            {
                return;
            }

            try
            {
                var client = _grpcClientFactory.CreateGrpcClient();
                _snapshotStream = client.SubscribeSnapshots(new Empty());

                while (await _snapshotStream.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var logString = "Snapshot: " + _snapshotStream.ResponseStream.Current;
                    InvokeIfRequired(() =>
                    {
                        grpcStatusLabel.Text = logString;
                        AppendLog(logString);
                    });
                    Debug.WriteLine(logString);
                }
            }
            catch (Exception ex)
            {
                InvokeIfRequired(() => grpcStatusLabel.Text = "Error subscribing to snapshots: " + ex);
                _snapshotStream = null;
            }
        }

        private async Task SubscribeSummariesAsync()
        {
            // TODO: reconnect after error?
            if (_summariesStream != null)
            {
                return;
            }

            try
            {
                var client = _grpcClientFactory.CreateGrpcClient();
                _summariesStream = client.SubscribeSummaries(new Empty());

                while (await _summariesStream.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var logString = "Summary: " + _summariesStream.ResponseStream.Current.ToString();
                    InvokeIfRequired(() =>
                    {
                        grpcStatusLabel.Text = logString;
                        AppendLog(logString);
                    });
                    Debug.WriteLine(logString);
                }
            }
            catch (Exception ex)
            {
                InvokeIfRequired(() => grpcStatusLabel.Text = "Error subscribing to summaries: " + ex);
                _summariesStream = null;
            }
        }

        private async void ForceSyncButton_Click(object sender, EventArgs e)
        {
            try
            {
                var client = _grpcClientFactory.CreateGrpcClient();
                _ = await client.StartSyncAsync(new Empty());
            }
            catch (Exception ex)
            {
                InvokeIfRequired(() => grpcStatusLabel.Text = "Error syncing: " + ex);
            }
        }

        private void AppendLog(string logString)
        {
            logTextBox.AppendText(DateTime.Now + ": " + logString + Environment.NewLine);
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
    }
}
