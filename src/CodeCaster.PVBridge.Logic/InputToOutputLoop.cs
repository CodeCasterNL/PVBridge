using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// Gets instantiated per input-to-output configuration, handles a continuous loop that periodically reads input and writes output.
    /// </summary>
    public class InputToOutputLoop : IInputToOutputLoop
    {
        private static int _taskId;

        private readonly IInputToOutputWriter _ioWriter;
        private readonly DataProviderConfiguration _inputProvider;
        private readonly DataProviderConfiguration[] _outputProviders;
        private readonly TimeSpan _maxStatusAge;
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
            _maxStatusAge = TimeSpan.FromMinutes(5);

            _taskStatus = new InputToOutputLoopStatus(logger, Interlocked.Increment(ref _taskId), syncStart, _maxStatusAge);
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
                    // Set the next loop start already.
                    var continueAt = DateTime.Now + _maxStatusAge;

                    // For each iteration, check if we were suspended or if we're behind on something. Then fix that.
                    switch (_taskStatus.UpdateState())
                    {
                        // We're up to date until now, sync the current status.
                        case StateMachine.SyncLiveStatus:
                            await SyncCurrentStatusAsync();

                            break;

                        // We were just installed, or started, or down for some time, or a day has passed.
                        case StateMachine.SyncBacklog:
                            await SyncBacklogAsync();

                            break;
                    }

                    // Update the state again, one above may have run.
                    if (_taskStatus.UpdateState() == StateMachine.Wait)
                    {
                        continueAt = _taskStatus.GetWaitTimeAsync();
                    }

                    var waitTime = continueAt - DateTime.Now;

                    _logger.LogDebug("Sleeping for {waitTime}", waitTime);

                    using var loopStoppingToken = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken, _loopWaitCancelToken.Token);

                    await Task.Delay(waitTime, loopStoppingToken.Token);
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

        private async Task SyncBacklogAsync()
        {
            var since = _taskStatus.GetBacklogStart();

            var until = DateTime.Now;

            // PVOutput's limit is 50 records, 150 for donation. GoodWe's limit appears to work with 40, not tested further.
            // Don't push it, just sync per 31 days.

            const int daysPerBatch = 31;
            var batchCount = (int)Math.Ceiling((until - since).TotalDays / daysPerBatch);

            for (int i = 0; i < batchCount; i++)
            {
                var batchNumber = i + 1;
                var batchStart = since.AddDays(i * daysPerBatch);
                var batchEnd = new[] { DateTime.Now, since.AddDays(daysPerBatch) }.Min();

                _logger.LogDebug("Checking backlog from {monthStart} to {monthEnd} ({batchNumber}/{batchCount})", batchStart, batchEnd, batchNumber, batchCount);

                // Input summaries aren't available until after 03:00 ~ 04:00 (local) the next day.
                var inputSummaries = await _ioWriter.GetSummariesAsync(_inputProvider, batchStart, batchEnd, _stoppingToken);

                _taskStatus.HandleApiResponse(inputSummaries);

                // TODO: when any of the days is null, let caller get latest output(s) from that day as cheaply as possible and get the total from there.
                // See #23.

                // Consider an empty response with 200 OK succesful here, so we continue.
                if (inputSummaries.Status is not ApiResponseStatus.Succeeded)
                {
                    return;
                }

                foreach (var outputConfig in _outputProviders)
                {
                    await SyncBatchAsync(inputSummaries, outputConfig, batchStart, batchEnd);
                }
            }
        }

        private async Task SyncBatchAsync(ApiResponse<IReadOnlyCollection<DaySummary>> inputSummaries, DataProviderConfiguration outputConfig, DateTime batchStart, DateTime batchEnd)
        {
            var outputSummaries = await _ioWriter.GetSummariesAsync(outputConfig, batchStart, batchEnd, _stoppingToken);

            _taskStatus.HandleApiResponse(outputSummaries);

            // Output summaries are optional. "No data" (no summary for today or yesterday) gets returned as BadRequest,
            // so only check for rate limit or error here.
            if (outputSummaries.Status is ApiResponseStatus.RateLimited or ApiResponseStatus.Failed)
            {
                // Try again later.
                return;
            }

            var periodResult = await _ioWriter.SyncPeriodAsync(_inputProvider, outputConfig, batchStart, batchEnd, inputSummaries.Response, outputSummaries.Response, _stoppingToken);

            if (!periodResult.Any())
            {
                // No days were synced, continue to next output.
                return;
            }

            foreach (var dayResult in periodResult)
            {
                // The first non-success status should be the last status.
                _taskStatus.HandleApiResponse(dayResult);

                if (dayResult.IsSuccessful && dayResult.Response.SyncedAt.HasValue)
                {
                    var day = DateOnly.FromDateTime(dayResult.Response.Day);

                    // Report as synced, so we don't report the same day twice.
                    _taskStatus.DataSynced(day, dayResult.Response.SyncedAt.Value);

                    continue;
                }

                // When running before dawn, there's no summary for today.
                if (dayResult == periodResult.Last() && (dayResult.Response?.DailyGeneration).GetValueOrDefault() == 0)
                {
                    _taskStatus.StaleDataReceived(batchEnd);
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
                _logger.LogDebug("Received old data, skipping: {currentStatus}", currentStatus);

                _taskStatus.StaleDataReceived(currentStatus.TimeTaken);

                return;
            }

            // We've seen that one before, the inverter is probably off.
            if (_lastStatus?.TimeTaken == currentStatus.TimeTaken)
            {
                _logger.LogDebug("Status is equal to the previous, skipping: {currentStatus}", currentStatus);

                _taskStatus.StaleDataReceived(currentStatus.TimeTaken);

                return;
            }

            if (currentStatus.ActualPower is null or 0)
            {
                _logger.LogDebug("No actual power, might be either stale, dark or disconnected: {currentStatus}", currentStatus);

                _taskStatus.StaleDataReceived(currentStatus.TimeTaken);

                return;
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
                    var day = DateOnly.FromDateTime(currentStatus.TimeTaken);
                    _taskStatus.DataSynced(day, currentStatus.TimeTaken);
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
