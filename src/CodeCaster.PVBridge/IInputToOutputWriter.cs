﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge
{
    /// <summary>
    /// Knows provider by type, operates on a specific config. May cache per config per day.
    /// </summary>
    public interface IInputToOutputWriter
    {
        /// <summary>
        /// Read current status from input.
        /// </summary>
        Task<ApiResponse<Snapshot>> GetLiveSnapshotAsync(DataProviderConfiguration inputConfig, CancellationToken cancellationToken);

        /// <summary>
        /// Read summaries (statistics) for the given days, either from an input or an output.
        /// </summary>
        Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetSummariesAsync(DataProviderConfiguration providerConfig, DateTime since, DateTime until, CancellationToken cancellationToken);

        /// <summary>
        /// Send a snapshot to the output.
        /// </summary>
        Task<ApiResponse<Snapshot>> WriteSnapshotAsync(string inputNameOrType, DataProviderConfiguration outputConfiguration, Snapshot currentStatus, CancellationToken cancellationToken);

        /// <summary>
        /// Sync a range of statuses, returning those who were sent and successfully received.
        /// </summary>
        /// <param name="force">When true, attempts to sync the day anyway even when it's too old</param>
        Task<ApiResponse<IReadOnlyCollection<Snapshot>>> SyncPeriodDetailsAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime day, bool force, CancellationToken cancellationToken);
        /// <summary>
        /// Send a day summary to the output.
        /// </summary>
        Task<ApiResponse<DaySummary>> WriteDaySummaryAsync(DataProviderConfiguration outputConfig, DaySummary inputSummary, DaySummary? outputSummary, CancellationToken cancellationToken);

        /// <summary>
        /// Returns whether snapshots for the given day can be written to the configured output.
        /// </summary>
        bool CanWriteDetails(DataProviderConfiguration outputConfig, DateTime day);

        /// <summary>
        /// Returns whether a summary for the given day can be written to the configured output.
        /// </summary>
        bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day);

        /// <summary>
        /// Asynchronously synchronizes a period of time (can be minutes to days) from input to output. 
        /// </summary>
        IAsyncEnumerable<(DateOnly day, ApiResponse<DaySummary>)> SyncPeriodAsync(DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime since, DateTime until, IReadOnlyCollection<DaySummary>? inputSummaries, IReadOnlyCollection<DaySummary>? outputSummaries, CancellationToken stoppingToken);
    }
}
