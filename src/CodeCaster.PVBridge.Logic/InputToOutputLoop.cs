using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// Gets instantiated per input-to-output configuration, handles a continuous loop that periodically reads input and writes output.
    /// </summary>
    public class InputToOutputLoop : IInputToOutputLoop
    {
        private static int _taskId;

        /// <summary>
        /// Tick every 2.something minutes (because we're bound to drift), sync every 5th minute (see _status._maxStatusAge).
        /// </summary>
        private readonly TimeSpan _mainLoopInterval = TimeSpan.FromSeconds(123);

        private readonly IInputToOutputWriter _ioWriter;
        private readonly DataProviderConfiguration _inputProvider;
        private readonly DataProviderConfiguration[] _outputProviders;

        private readonly InputToOutputLoopStatus _taskStatus;

        private readonly ILogger<IInputToOutputLoop> _logger;

        private Snapshot? _lastStatus;

        private CancellationToken _stoppingToken;
        private CancellationTokenSource _loopWaitCancelToken = new();

        public InputToOutputLoop(
            ILogger<IInputToOutputLoop> logger,
            IInputToOutputWriter ioWriter,
            DataProviderConfiguration inputProvider,
            DataProviderConfiguration[] outputProviders,
            DateTime syncStart
        )
        {
            _logger = logger;

            _ioWriter = ioWriter;

            _inputProvider = inputProvider;
            _outputProviders = outputProviders;

            // TODO: Math.Max(input.Resolution, output.Resolution)
            var maxStatusAge = TimeSpan.FromMinutes(5);

            _taskStatus = new InputToOutputLoopStatus(logger, Interlocked.Increment(ref _taskId), syncStart, maxStatusAge);
        }

        public void StatusSyncRequested(object? sender, EventArgs e)
        {
            _logger.LogInformation("Status sync requested");

            ContinueLoop();
        }

        private void ContinueLoop()
        {
            using var oldToken = _loopWaitCancelToken;
            
            _loopWaitCancelToken = new CancellationTokenSource();

            oldToken.Cancel();
        }

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            // Main loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // For each iteration, check if we were suspended or if we're behind on something. Then fix that.
                    switch (_taskStatus.UpdateState())
                    {
                        // Stale data/API limit/error, retry later.
                        case StateMachine.Wait:
                            // Do nothing, enter next wait.

                            break;

                        // We're up to date until now, sync the current status.
                        case StateMachine.SyncLiveStatus:
                            await SyncCurrentStatusAsync();

                            break;

                        // We were just installed, or started, or down for some time, or a day has passed.
                        case StateMachine.SyncBacklog:
                            await SyncBacklogAsync();

                            break;

                        default:
                            throw new ArgumentException("This should not happen.");
                    }

                    _logger.LogDebug("Sleeping for {mainLoopInterval}", _mainLoopInterval);

                    using var loopStoppingToken = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken, _loopWaitCancelToken.Token);

                    await Task.Delay(_mainLoopInterval, loopStoppingToken.Token);
                }
                catch (OperationCanceledException) when (_loopWaitCancelToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Sleeping canceled, continuing loop");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Cancellation requested, stopping loop");

                    break;
                }
            }
        }

        /// <summary>
        /// TODO: the cyclomatic complexity is bonkers.
        /// </summary>
        private async Task SyncBacklogAsync()
        {
            var since = _taskStatus.GetBacklogStart();
            
            var until = DateTime.Now;

            // PVOutput's limit is 50 records, 150 for donation. GoodWe's limit appears to work with 40, not tested further.
            // Don't push it, just sync per 31 days.

            const int daysPerMonth = 31;
            var months = (int)Math.Ceiling((until - since).TotalDays / daysPerMonth);

            for (int m = 0; m < months; m++)
            {
                var monthStart = since.AddDays(m * daysPerMonth);
                var monthEnd = new[] { DateTime.Now, since.AddDays(daysPerMonth) }.Min();

                _logger.LogDebug("Checking backlog from {monthStart} to {monthEnd} ({month}/{months})", monthStart, monthEnd, m + 1, months);

                // Input summaries aren't available until after 03:00 ~ 04:00 (local) the next day.
                var inputSummaries = await _ioWriter.GetSummariesAsync(_inputProvider, monthStart, monthEnd, _stoppingToken);

                _taskStatus.HandleApiResponse(inputSummaries);

                // Consider an empty response with 200 OK succesful here, so we continue.
                if (inputSummaries.Status is not ApiResponseStatus.Succeeded)
                {
                    return;
                }

                foreach (var outputConfig in _outputProviders)
                {
                    var outputSummaries = await _ioWriter.GetSummariesAsync(outputConfig, monthStart, monthEnd, _stoppingToken);

                    _taskStatus.HandleApiResponse(outputSummaries);

                    // "No data" (no summary for today or yesterday) gets returned as an error, so only check for rate limit here.
                    if (outputSummaries.Status is ApiResponseStatus.RateLimited)
                    {
                        return;
                    }

                    var days = (int)Math.Ceiling((monthEnd - monthStart).TotalDays);

                    _logger.LogDebug("Checking backlog from {monthStart} to {monthEnd}", monthStart, monthEnd);

                    for (int d = 0; d < days; d++)
                    {
                        var day = monthStart.AddDays(d);

                        // When we shut down and continue the next day, start syncing today at 00:00.
                        // Otherwise, it may be a backlog sync of just today; then sync since monthStart's time.
                        if (d > 0 && day.Date == DateTime.Today)
                        {
                            day = day.Date;
                        }

                        DaySummary? inputSummary = null;
                        DaySummary? outputSummary = null;

                        // When a day is 6 hours old, we'd expect a summary by then.
                        var shouldHaveSummary = day < DateTime.Now.AddHours(-(24 + 6));
                        if (shouldHaveSummary)
                        {
                            inputSummary = inputSummaries.Response?.FirstOrDefault(s => s.Day == day.Date);
                            outputSummary = outputSummaries.Response?.FirstOrDefault(s => s.Day == day.Date);

                            // API data seems to have gaps in its summaries sometimes, but they do catch up. Usually.
                            if ((inputSummary?.DailyGeneration).GetValueOrDefault() == 0)
                            {
                                if (d == 0 && m == 0)
                                {
                                    // We haven't received data before for this backlog sync.
                                    _logger.LogWarning("No input summary data for {input} on {day}, probably API connectivity errors, backing off", _inputProvider.NameOrType, day.LoggableDayName());
                                    
                                    _taskStatus.HandleApiResponse(ApiResponse.RateLimited(DateTime.Now.AddMinutes(15)));

                                    return;
                                }
                            }

                            // When input and output are (pretty much) equal, don't sync this day
                            if (inputSummary != null && outputSummary != null && Math.Abs(inputSummary.DailyGeneration!.Value - outputSummary.DailyGeneration.GetValueOrDefault()) < 0.1d)
                            {
                                _logger.LogDebug("{day} is already synced ({wH} on both sides), skipping", day.LoggableDayName(), inputSummary.DailyGeneration.Value.FormatWattHour());

                                _taskStatus.DataSynced(day);

                                continue;
                            }
                        }

                        // Should not happen (_state should be SyncLiveStatus), but alas - we'll have at most one snapshot, get that.
                        if (day > DateTime.Now - _mainLoopInterval)
                        {
                            await SyncCurrentStatusAsync();

                            return;
                        }

                        // Otherwise, sync from `day`, which can be today at any time < now - main loop interval, or any earlier day.
                        var dayResult = await _ioWriter.SyncPeriodDetailsAsync(_inputProvider, outputConfig, day, _stoppingToken);

                        _taskStatus.HandleApiResponse(dayResult);

                        if (dayResult.Status == ApiResponseStatus.Succeeded && dayResult.Response?.Count == 0 && day.Date == DateTime.Today)
                        {
                            _taskStatus.StaleDataReceived();
                        }

                        // Failed or no results.
                        if (!dayResult.IsSuccessful)
                        {
                            return;
                        }

                        var newestSnapshot = dayResult.Response.OrderByDescending(s => s.TimeTaken).First();

                        if (day.Date == DateTime.Today)
                        {
                            // We're up to date until now.
                            _lastStatus = newestSnapshot;
                        }
                        else
                        {
                            // When before today, report the summary (or last status), then continue.
                            if (inputSummary == null || inputSummary.DailyGeneration < newestSnapshot.DailyGeneration)
                            {
                                inputSummary = new DaySummary
                                {
                                    Day = newestSnapshot.TimeTaken.Date,
                                    DailyGeneration = newestSnapshot.DailyGeneration,
                                };
                            }

                            var summaryResponse = await _ioWriter.WriteDaySummaryAsync(outputConfig, inputSummary, outputSummary, _stoppingToken);

                            _taskStatus.HandleApiResponse(summaryResponse);

                            if (!summaryResponse.IsSuccessful)
                            {
                                _logger.LogError("Failed to write summary for {day}: {status}", day.LoggableDayName(), summaryResponse.Status);

                                return;
                            }
                        }

                        // Report after writing summary, so we don't report the same day twice.
                        _taskStatus.DataSynced(newestSnapshot.TimeTaken);
                    }
                }
            }
        }

        private async Task SyncCurrentStatusAsync()
        {
            _logger.LogDebug("Syncing current status");

            var snapshotResponse = await _ioWriter.GetLiveSnapshotAsync(_inputProvider, _stoppingToken);

            _taskStatus.HandleApiResponse(snapshotResponse);

            if (!snapshotResponse.IsSuccessful)
            {
                return;
            }

            var currentStatus = snapshotResponse.Response;

            // GoodWe can report a stale state for hours after shutdown.
            if (currentStatus.TimeTaken.Date < DateTime.Now.Date)
            {
                _logger.LogDebug("Received old data ({timeTaken}), skipping", currentStatus.TimeTaken);

                _taskStatus.StaleDataReceived();

                return;
            }

            // We've seen that one before, the inverter is probably off.
            if (_lastStatus?.TimeTaken == currentStatus.TimeTaken)
            {
                _logger.LogDebug("Status is equal to the previous status ({timeTaken}), skipping", currentStatus.TimeTaken);

                _taskStatus.StaleDataReceived();

                return;
            }

            if (currentStatus.ActualPower == 0)
            {
                _logger.LogDebug("No actual power");

                _taskStatus.StaleDataReceived();
            }

            _lastStatus = currentStatus;

            foreach (var output in _outputProviders)
            {
                var statusResponse = await _ioWriter.WriteSnapshotAsync(_inputProvider.NameOrType, output, currentStatus, _stoppingToken);

                _taskStatus.HandleApiResponse(statusResponse);

                // TODO: AllOk/PartialFail/TotalFail per output. That way when one output fails, the other gets written to.
                // But if one output fails, the next attempt will have to read the input data again anyway... 
                // For that to be useful, we need more caching, so we don't have to fetch input again which we recently read.
                // But then we need to invalidate that cache sometimes.

                if (statusResponse.IsSuccessful)
                {
                    _taskStatus.DataSynced(currentStatus.TimeTaken);
                }
            }
        }

        public void Suspend()
        {
            _taskStatus.Suspend();
        }

        public void Resume()
        {
            _taskStatus.Resume();

            // Restart the main loop.
            ContinueLoop();
        }
    }
}
