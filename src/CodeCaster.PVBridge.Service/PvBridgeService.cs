using CodeCaster.PVBridge.Logic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Utils;
using CodeCaster.PVBridge.Service.Grpc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using CodeCaster.PVBridge.Configuration.Protection;
using CodeCaster.WindowsServiceExtensions.Service;

namespace CodeCaster.PVBridge.Service
{
    /// <summary>
    /// Loads of boilerplate just to run background tasks that regularly send data from input (currently: GoodWe) to output (currently: PVOutput, CSV).
    ///
    /// A task is executed by an <see cref="IInputToOutputLoop"/>, one for each configuration (usually one input to one output).
    /// </summary>
    public class PvBridgeService : WindowsServiceBackgroundService
    {
        private readonly IClock _clock;
        private readonly ILogger<PvBridgeService> _logger;
        
        /// <summary>
        /// Locks configuration reads and writes.
        /// </summary>
        private readonly SemaphoreSlim _configurationSemaphore;

        private bool _configurationNeedsReload;

        private IInputToOutputLoop[]? _inputToOutputLoops;

        private CancellationTokenSource? _taskStopToken;

        
        /// <summary>
        /// One for all loops, ILogger{T}'s and log4net's implementation are thread safe.
        /// </summary>
        private readonly ILogger<IInputToOutputLoop> _loopLogger;

        private readonly IClientAndServiceMessageBroker _messageBroker;

        private readonly IInputToOutputWriter _ioWriter;
        private readonly IOptionsMonitor<BridgeConfiguration> _options;

        // Bypass System.CommandLine, do our own parsing for this one.
        private static bool ShouldRunIdle => Environment.GetCommandLineArgs().Any(a => a.Equals("--IDLE", StringComparison.InvariantCultureIgnoreCase));

        public PvBridgeService(
            ILoggerFactory loggerFactory,
            IClock clock,
            IHostLifetime hostLifetime,
            IOptionsMonitor<BridgeConfiguration> options,
            IClientAndServiceMessageBroker messageBroker,
            IInputToOutputWriter ioWriter
        )
            : base(loggerFactory.CreateLogger<PvBridgeService>(), hostLifetime)
        {
            _clock = clock;
            _options = options;
            _ioWriter = ioWriter;
            _messageBroker = messageBroker;
            _logger = loggerFactory.CreateLogger<PvBridgeService>();

            _loopLogger = loggerFactory.CreateLogger<IInputToOutputLoop>();

            RefreshTaskStopToken();

            // Used to disallow concurrent configuration load and read.
            _configurationSemaphore = new SemaphoreSlim(1, 1);

            _options.OnChange((_, _) =>
            {
                _logger.LogDebug("Configuration file modified, reloading next iteration");

                _configurationNeedsReload = true;
            });

            _configurationNeedsReload = true;
        }

        /// <summary>
        /// Entry point of the BackgroundService.
        /// </summary>
        protected override async Task TryExecuteAsync(CancellationToken serviceStopToken)
        {
            RegisterTokenCancelLogger(nameof(serviceStopToken), serviceStopToken);

            do
            {
                if (_taskStopToken == null || _taskStopToken.IsCancellationRequested)
                {
                    RefreshTaskStopToken();
                }

                using var combinedStoppingToken = CancellationTokenSource.CreateLinkedTokenSource(_taskStopToken!.Token, serviceStopToken);

                RegisterTokenCancelLogger(nameof(combinedStoppingToken), combinedStoppingToken.Token);

                try
                {
                    if (_configurationNeedsReload || _inputToOutputLoops?.Any() != true)
                    {
                        await ReloadConfigurationAsync(combinedStoppingToken.Token);
                    }

                    _logger.LogInformation("Entering main loop with {taskCount}", _inputToOutputLoops?.Length.SIfPlural("task") ?? "null tasks (run the PVBridge Configuration UI to check the configuration)");

                    // Run the loop for each I/O configuration. Usually, there'll be one input having one output. YAGNI and all, but WIP.
                    // ReSharper disable once AccessToDisposedClosure - we wait for all tasks.
                    await Task.WhenAll(_inputToOutputLoops!.Select(t => t.RunAsync(combinedStoppingToken.Token)));
                }
                catch (OperationCanceledException ex) when (serviceStopToken.IsCancellationRequested)
                {
                    const string logString = "Worker process was canceled";
                    _logger.LogTrace(ex, logString);
                    _logger.LogInformation(logString);
                }
                catch (Exception ex)
                {
                    // Accept these: the next iteration will check the appropriate tokens again.
                    if (ex is OperationCanceledException or TaskCanceledException)
                    {
                        _logger.LogDebug("Canceled: _taskStop: {taskStop}, combined: {combined}", _taskStopToken.IsCancellationRequested, combinedStoppingToken.IsCancellationRequested);

                        continue;
                    }

                    // Fail fast on any other exception. We don't want to go haywire.
                    // TODO: report error to message bus (UI).
                    throw;
                }
            }
            while (!serviceStopToken.IsCancellationRequested);

            // Service cancellation is requested, we're the last ones out, turn off the ligt.
            await base.StopAsync(serviceStopToken);
        }

        private void RefreshTaskStopToken()
        {
            _taskStopToken?.Dispose();

            _taskStopToken = new CancellationTokenSource();
            RegisterTokenCancelLogger(nameof(_taskStopToken), _taskStopToken.Token);
        }

        private void RegisterTokenCancelLogger(string tokenName, CancellationToken token)
            => token.Register(() => _logger.LogDebug("{token} cancellation requested", tokenName));

        private async Task ReloadConfigurationAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Entering configuration semaphore");
            if (!await _configurationSemaphore.WaitAsync(TimeSpan.FromMilliseconds(1), cancellationToken))
            {
                _logger.LogDebug("Configuration already being loaded, skipping");

                // Someone else is loading the config. Wait until they're done.
                await _configurationSemaphore.WaitAsync(cancellationToken);

                _logger.LogTrace("Releasing configuration semaphore");

                _configurationSemaphore.Release();

                return;
            }

            _logger.LogTrace("Configuration semaphore entered");


            try
            {
                // Debounce, OnChange()-triggering config file edits usually come in pairs, on my system ~500ms apart.
                // Wait first, so we're bound to get the latest version.
                await Task.Delay(1000, cancellationToken);

                if (!_configurationNeedsReload)
                {
                    _logger.LogDebug("Configuration doesn't need reloading");

                    _logger.LogTrace("Releasing configuration semaphore");
                    _configurationSemaphore.Release();
                    _logger.LogTrace("Configuration semaphore released");

                    return;
                }

                if (_inputToOutputLoops != null)
                {
                    _logger.LogDebug("Canceling existing tasks");

                    // Unsubscribe to prevent memory leaks.
                    foreach (var inputToOutputLoop in _inputToOutputLoops)
                    {
                        _messageBroker.StatusSyncRequested -= inputToOutputLoop.StatusSyncRequested;
                    }

                    // This stops the loops. May re-enter this method, hence the semaphore.
                    _taskStopToken!.Cancel();

                }

                _logger.LogInformation("Loading configuration");

                // This reads the current version.
                var configuration = _options.CurrentValue;

                await ConfigurationProtector.UnprotectAsync(configuration);

                var newTasks = GetNewTasks(configuration);

                _inputToOutputLoops = newTasks.ToArray();

                _logger.LogDebug("Loaded {tasks}", _inputToOutputLoops.Length.SIfPlural("task"));

                _configurationNeedsReload = false;
            }
            finally
            {
                _logger.LogTrace("Releasing configuration semaphore");
                _configurationSemaphore.Release();
                _logger.LogTrace("Configuration semaphore released");
            }
        }

        private List<IInputToOutputLoop> GetNewTasks(BridgeConfiguration? configuration)
        {
            var newTasks = new List<IInputToOutputLoop>();

            if (ShouldRunIdle || configuration == null || !configuration.InputToOutput.Any())
            {
                _logger.LogInformation("Nothing to do, running idle");

                newTasks.Add(new IdleInputToOutputLoop(_loopLogger, _messageBroker));

                return newTasks;
            }

            try
            {
                var loopConfigurations = configuration.ReadConfiguration();

                _logger.LogDebug("Configuring {loopCount} with {outputCount}",
                    loopConfigurations.Count.SIfPlural("loop"), loopConfigurations.Sum(l => l.Item2.Length).SIfPlural("output"));

                foreach (var (input, outputs) in loopConfigurations)
                {
                    _logger.LogInformation("Configuring {input} to {outputs}", input.NameOrType, string.Join(", ", outputs.Select(o => o.NameOrType)));

                    // TODO: premium accounts can sync further back, see #10.
                    var syncStart = DateTime.Today.AddDays(-13);

                    var loop = new InputToOutputLoop(_loopLogger, _clock, _ioWriter, input, outputs, syncStart);

                    _messageBroker.StatusSyncRequested += loop.StatusSyncRequested;

                    newTasks.Add(loop);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Configuration error");

                newTasks.Clear();

                newTasks.Add(new IdleInputToOutputLoop(_loopLogger, _messageBroker));
            }

            return newTasks;
        }

        public override void OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            _logger.LogDebug("OnPowerEvent: {powerStatus}", powerStatus);

            // Signal all tasks of the power state change.
            foreach (var t in _inputToOutputLoops!)
            {
                if (powerStatus == PowerBroadcastStatus.Suspend)
                {
                    t.Suspend();
                }

                if (powerStatus.In(PowerBroadcastStatus.ResumeSuspend, PowerBroadcastStatus.ResumeAutomatic))
                {
                    t.Resume();
                }
            }
        }
    }
}
