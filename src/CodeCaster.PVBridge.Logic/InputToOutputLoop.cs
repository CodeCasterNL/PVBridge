using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Logic.Status;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// Gets instantiated per input-to-output(s) configuration, handles a continuous loop that periodically reads input and writes output.
    /// </summary>
    public class InputToOutputLoop : IInputToOutputLoop
    {
        private readonly ILogger<IInputToOutputLoop> _logger;
        private readonly IClock _clock;

        private readonly IInputToOutputWriter _ioWriter;
        private readonly DataProviderConfiguration _inputProvider;
        private readonly DataProviderConfiguration[] _outputProviders;
        private readonly LiveStatus _liveStatus;
        private readonly BacklogStatus _backlogStatus;
        
        private CancellationToken _stoppingToken;
        private CancellationTokenSource _loopWaitCancelToken = new();

        public InputToOutputLoop(
            ILogger<IInputToOutputLoop> logger,
            IClock clock,
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
            var statusResolution = TimeSpan.FromMinutes(5);

            _clock = clock;
            _liveStatus = new LiveStatus(logger, clock, statusResolution);
            _backlogStatus = new BacklogStatus(logger, clock, syncStart, statusResolution);
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
                    var backlogState = _backlogStatus.GetState();

                    if (!backlogState.ShouldRetry)
                    {
                        backlogState = await SyncBackLogAsync();
                    }

                    var liveState = _liveStatus.GetState();

                    if (!liveState.ShouldRetry)
                    {
                        liveState = await SyncCurrentStatusAsync();
                    }

                    var now = _clock.Now;

                    // TODO: determine state better. 

                    // Calculate continuation time.
                    var continueAt = new[]
                    {
                        // This one's for sanity
                        now.AddMinutes(5),

                        // This the most likely
                        liveState.ContinueAt,

                        // This the most heavy, it should time itself.
                        backlogState.ContinueAt,
                    }.Where(d => d.HasValue && d.Value > now).Select(d => d!.Value).Min();

                    var waitTime = continueAt - now;

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

        private async Task<State> SyncCurrentStatusAsync()
        {
            _logger.LogDebug("Syncing current status");

            var snapshotResponse = await _ioWriter.GetLiveSnapshotAsync(_inputProvider, _stoppingToken);

            var inputState = _liveStatus.HandleSnapshotReadResponse(_inputProvider, snapshotResponse);

            // Can't retrieve input, try again later.
            if (inputState.ShouldRetry)
            {
                return inputState;
            }

            var snapshot = snapshotResponse.Response!;

            State resultState = inputState;

            foreach (var output in _outputProviders)
            {
                var statusResponse = await _ioWriter.WriteSnapshotAsync(_inputProvider.NameOrType, output, snapshot, _stoppingToken);

                // TODO: don't hammer the same concrete provider on 500(/0)?
                var outputState = _liveStatus.HandleSnapshotWriteResponse(output, statusResponse);

                // Continue at the earliest moment.
                if (outputState.ShouldRetry && (!resultState.ShouldRetry || outputState.ContinueAt < resultState.ContinueAt))
                {
                    resultState = outputState;
                }
            }

            return resultState;
        }

        private Task<State> SyncBackLogAsync()
        {
            var (state, days) = _backlogStatus.GetBacklog();

            if (state.ShouldRetry)
            {
                return Task.FromResult(state);
            }

            // This should not happen, state should be set. TODO: test that.
            if (!days.Any())
            {
                return Task.FromResult(State.Wait(_clock.Now.AddMinutes(120)));
            }

            // TODO: today's special
            var since = days.First().Day.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
            var until = days.Last().Day.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));

            return SyncBatchAsync(since, until);
        }

        /*
         * If InputProvider GoodWe reports 0 data for a day, that might be genuine, but it also can be a temporary statistics or API error.
         * This usually resolves itself within a few days, but we should still try multiple times a day with a few hours' interval at most.
         */
        private async Task<State> SyncBatchAsync(DateTime since, DateTime until)
        {
            //Try again two hours and some odd minutes from now if nothing needs to be done.
            var earliestRetry = _clock.Now.Truncate(TimeSpan.FromHours(1)).AddHours(2).AddMinutes(7);

            var backlogState = State.Wait(earliestRetry);

            // PVOutput's limit is 50 records, 150 for donation. GoodWe's limit appears to work with 40, not tested further.
            // Don't push it, just sync per 31 days.

            const int daysPerBatch = 31;
            var batchCount = (int)Math.Ceiling((until - since).TotalDays / daysPerBatch);

            for (int i = 0; i < batchCount; i++)
            {
                var batchNumber = i + 1;
                var batchStart = since.AddDays(i * daysPerBatch);
                var batchEnd = new[] { _clock.Now, since.AddDays(daysPerBatch) }.Min();

                _logger.LogDebug("Checking backlog from {monthStart} to {monthEnd} ({batchNumber}/{batchCount})", batchStart, batchEnd, batchNumber, batchCount);

                var inputSummaries = await _ioWriter.GetSummariesAsync(_inputProvider, batchStart, batchEnd, _stoppingToken);

                var inputState = _backlogStatus.HandleSummariesReadResponse(_inputProvider, inputSummaries);

                if (inputState.ShouldRetry)
                {
                    return inputState;
                }
                foreach (var outputConfig in _outputProviders)
                {
                    var outputSummaries = await _ioWriter.GetSummariesAsync(outputConfig, batchStart, batchEnd, _stoppingToken);

                    var outputState = _backlogStatus.HandleSummariesReadResponse(outputConfig, outputSummaries);

                    if (outputState.ShouldRetry)
                    {
                        return outputState;
                    }

                    // This syncs the missing days and reports back per day.
                    var asyncDaySync = _ioWriter.SyncPeriodAsync(_inputProvider, outputConfig, batchStart, batchEnd, inputSummaries.Response, outputSummaries.Response, _stoppingToken);

                    // Stops looping on rate limit hit, comtinues on error.
                    await foreach (var (day, dayResult) in asyncDaySync.WithCancellation(_stoppingToken))
                    {
                        var dayState = _backlogStatus.HandleDayWrittenResponse(outputConfig, day, dayResult);
                        
                        // TODO: duplicate logic, _what_ should Handle above return?
                        if (dayState.ShouldRetry && backlogState.ContinueAt > dayState.ContinueAt)
                        {
                            backlogState = dayState;
                        }
                    }
                }
            }

            return backlogState;
        }
        
        public void Suspend()
        {
            _liveStatus.Suspend();
            _backlogStatus.Suspend();
        }

        public void Resume()
        {
            _liveStatus.Resume();
            _backlogStatus.Resume();

            // Restart the main loop.
            ContinueLoop();
        }
    }
}
