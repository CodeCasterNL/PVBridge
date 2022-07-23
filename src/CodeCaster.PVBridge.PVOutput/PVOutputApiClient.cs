using CodeCaster.PVBridge.Utils;
using PVOutput.Net.Builders;
using PVOutput.Net.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PVOutput.Net.Responses;

namespace CodeCaster.PVBridge.PVOutput
{
    // ReSharper disable once InconsistentNaming
    public class PVOutputApiClient : CachingSummaryProvider, IOutputWriter
    {
        private const int BatchSize = 30;

        private readonly StatusPostBuilder<IStatusPost> _statusBuilder;
        private readonly StatusPostBuilder<IBatchStatusPost> _batchStatusBuilder;
        private readonly OutputPostBuilder _batchOutputBuilder;

        private readonly JsonSerializerOptions _responseLogJsonFormat = new() { WriteIndented = true };

        private readonly PVOutputApiRateInformation _apiRateInformation = new();

        private readonly Dictionary<string, PVOutputWrapper> _apiClientCache = new CaseInsensitiveDictionary<PVOutputWrapper>();
        private readonly string? _jsonDataDirectory;

        public PVOutputApiClient(ILogger<PVOutputApiClient> logger, IOptions<LoggingConfiguration>? loggingConfig, IClientMessageBroker messageBroker)
            : base("PVOutput", logger, messageBroker)
        {
            _jsonDataDirectory = loggingConfig?.Value?.JsonData?.CreateAndGetDataDirectory();

            _statusBuilder = new StatusPostBuilder<IStatusPost>();
            _batchStatusBuilder = new StatusPostBuilder<IBatchStatusPost>();
            _batchOutputBuilder = new OutputPostBuilder();
        }

        public async Task<PVOutputSystem?> GetSystemAsync(PVOutputConfiguration outputConfig, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(outputConfig);

            var systemResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.System.GetOwnSystemAsync(cancellationToken), $"PVOutput-GetOwnSystem({apiClient.ApiClient.OwnedSystemId}-TODO-logparams)");

            var system = systemResponse.Response?.Value;

            return system == null ? null : new PVOutputSystem(apiClient.ApiClient.OwnedSystemId, system);
        }

        public async Task<ApiResponse> WriteStatusAsync(DataProviderConfiguration outputConfig, Snapshot snapshot, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(outputConfig);

            Logger.LogTrace("Mapping snapshot: {snapshot}", snapshot);
            var status = Mapper.Map(_statusBuilder, snapshot);

            var addStatusResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Status.AddStatusAsync(status, cancellationToken), "PVOutput-AddStatus");

            return addStatusResponse;
        }

        public async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> WriteDaySummariesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<DaySummary> summaries, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(outputConfig);

            // We can only send output (summary) data up till yesterday.
            var items = summaries.Where(s => s.Day < DateTime.Now)
                                 .Select(s =>
                                 {
                                     Logger.LogTrace("Mapping summary: {summary}", s);
                                     return Mapper.Map(_batchOutputBuilder, s);
                                 })
                                 .ToArray();

            Logger.LogDebug("Syncing {summaryCount} to PVOutput for system {systemId}", items.Length.SIfPlural("symmary", "summaries"), apiClient.ApiClient.OwnedSystemId);

            var daySummaries = new List<DaySummary>();

            foreach (var outputPostBatch in items.Batch(BatchSize))
            {
                var batchList = outputPostBatch.ToList();

                // TODO: donation only
                //addOutputResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.AddOutputsAsync(batchList, cancellationToken), "PVOutput-Output-AddBatch");
                //daySummaries.AddRange(batchList.Select(...)

                // Manually batch the batch.
                foreach (var outputPost in batchList)
                {
                    var daySummary = Mapper.Map(outputPost);

                    daySummaries.Add(daySummary);

                    var addOutputResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.AddOutputAsync(outputPost, cancellationToken), "PVOutput-Output-AddOutput");

                    if (!addOutputResponse.IsSuccessful)
                    {
                        return new ApiResponse<IReadOnlyCollection<DaySummary>>(addOutputResponse, daySummaries);
                    }

                    daySummary.SyncedAt = DateTime.Now;;
                }
            }

            return daySummaries;
        }

        public async Task<ApiResponse> WriteStatusesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<Snapshot> snapshots, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(outputConfig);

            var statuses = snapshots.Select(s =>
                {
                    Logger.LogTrace("Mapping status: {status}", s);

                    return Mapper.Map(_batchStatusBuilder, s);
                })
                .ToList();

            var batchResponse = ApiResponse.Succeeded;

            foreach (var statusBatch in statuses.Batch(BatchSize))
            {
                var items = statusBatch.ToArray();

                Logger.LogDebug("Syncing {statusCount} to PVOutput for system {systemId}", items.Length.SIfPlural("status", "statuses"), apiClient.ApiClient.OwnedSystemId);

                // TODO: implement SyncedAt
                batchResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Status.AddBatchStatusAsync(items, cancellationToken), "PVOutput-Status-AddBatch");

                if (batchResponse.Status != ApiResponseStatus.Succeeded)
                {
                    return batchResponse;
                }
            }

            return batchResponse;
        }

        protected override async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration outputConfig, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var yesterday = DateTime.Today.AddDays(-1);

            // PVOutput can't get summaries for the current day.
            if (until == null || until.Value.Date >= DateTime.Today)
            {
                until = yesterday;
            }

            // Backlog sync for today requested.
            if (since > yesterday)
            {
                // Holy
                return Array.Empty<DaySummary>();
            }

            var apiClient = GetApiClient(outputConfig);

            if (since == until)
            {
                Logger.LogDebug("Getting PVOutput summaries for {day} for system {systemId}", since, apiClient.ApiClient.OwnedSystemId);

                return await GetDaySummaryAsync(since, apiClient, cancellationToken);
            }

            Logger.LogDebug("Getting PVOutput summaries from {since} until {until} for system {systemId}", since, until, apiClient.ApiClient.OwnedSystemId);
            
            return await GetDaySummariesAsync(since, until, apiClient, cancellationToken);
        }

        private async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DateTime since, [DisallowNull] DateTime? until, PVOutputWrapper apiClient, CancellationToken cancellationToken)
        {
            var summariesResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.GetOutputsForPeriodAsync(since, until.Value, cancellationToken: cancellationToken), "PVOutput-Output-GetOutputsForPeriod");

            if (summariesResponse.Status == ApiResponseStatus.RateLimited)
            {
                return ApiResponse<IReadOnlyCollection<DaySummary>>.RateLimited(summariesResponse.RetryAfter!.Value);
            }

            if (summariesResponse.Response?.Values == null)
            {
                Logger.LogDebug("Empty GetOutputsForPeriod response");

                return Array.Empty<DaySummary>();
            }

            return summariesResponse.Response.Values.Select(s => new DaySummary
            {
                Day = s.OutputDate,
                DailyGeneration = s.EnergyGenerated,
            }).ToList();
        }

        private async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummaryAsync(DateTime since, PVOutputWrapper apiClient, CancellationToken cancellationToken)
        {
            var summaryResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.GetOutputForDateAsync(since, cancellationToken: cancellationToken), "PVOutput-Output-GetOutputForDate");

            if (summaryResponse.Status == ApiResponseStatus.RateLimited)
            {
                return ApiResponse<IReadOnlyCollection<DaySummary>>.RateLimited(summaryResponse.RetryAfter!.Value);
            }

            if (summaryResponse.Response?.Value == null)
            {
                Logger.LogDebug("Empty GetOutputForDate response");

                return Array.Empty<DaySummary>();
            }

            return new[]
            {
                new DaySummary
                {
                    Day = summaryResponse.Response.Value.OutputDate,
                    DailyGeneration = summaryResponse.Response.Value.EnergyGenerated,
                }
            };
        }

        // How to save account premium status on configuration? From UI only? And as boolean? Or the end date?
        // Or from service, query through API (can we?) once per day when unknown or when close to expiration, then update config accordingly?

        public bool CanWriteDetails(DataProviderConfiguration outputConfig, DateTime day)
        {
            // Trigger configuration validation.
            _ = GetApiClient(outputConfig);

            // TODO: premium accounts can sync further back, see #10.
            return day <= DateTime.Now && day >= DateTime.Today.AddDays(-13);
        }

        public bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day)
        {
            // Trigger configuration validation.
            _ = GetApiClient(outputConfig);

            // TODO: premium accounts can sync further back.
            return day.Date <= DateTime.Today && day >= DateTime.Today.AddDays(-90);
        }

        [DebuggerStepThrough]
        private async Task<ApiResponse<TResponse>> TryHandleAndLogRequest<TResponse>(Func<Task<TResponse>> request, string jsonName)
            where TResponse : PVOutputBaseResponse
        {
            try
            {
                var response = await request();
                HandleResponse(response);
                await WriteJson(jsonName, response);
                return new ApiResponse<TResponse>(response);
            }
            catch (PVOutputException pvEx)
            {
#if DEBUG
                Debugger.Break();
#endif

                if (pvEx.StatusCode == HttpStatusCode.BadRequest)
                {
                    Logger.LogWarning(pvEx, "Bad Request");

                    return new ApiResponse<TResponse>(default, ApiResponseStatus.BadRequest);
                }

                Logger.LogError(pvEx, "Error syncing status to PV Output (statusCode: {statusCode})", pvEx.StatusCode);

                if (pvEx.StatusCode == HttpStatusCode.Forbidden)
                {
                    _apiRateInformation.Tripped = DateTime.Now;

                    return new ApiResponse<TResponse>(default, ApiResponseStatus.RateLimited)
                    {
                        // When we start up with an error, API rate limits are unknown. Retry at the next round hour.
                        RetryAfter = _apiRateInformation.LimitResetAt
                                     ?? DateTime.Now
                                         .AddMinutes(-DateTime.Now.Minute)
                                         .AddSeconds(-DateTime.Now.Second)
                                         .AddHours(1)
                    };
                }

                return new ApiResponse<TResponse>(default, ApiResponseStatus.Failed);
            }
        }

        private void HandleResponse<T>(T response)
            where T : PVOutputBaseResponse
        {
            var rateInfo = response.ApiRateInformation;
            if (rateInfo != null)
            {
                _apiRateInformation.MarkedStale = null;
                _apiRateInformation.Received = DateTime.Now;
                _apiRateInformation.CurrentLimit = rateInfo.CurrentLimit;
                _apiRateInformation.LimitResetAt = rateInfo.LimitResetAt.ToLocalTime();
                _apiRateInformation.LimitRemaining = rateInfo.LimitRemaining;

                if (rateInfo.LimitRemaining > 0)
                {
                    _apiRateInformation.Tripped = null;
                }

                Logger.LogDebug("PVOutput API Rate: CurrentLimit: {currentLimit}, LimitRemaining: {limitRemaining}, LimitResetAt: {limitResetAt}",
                    _apiRateInformation.CurrentLimit,
                    _apiRateInformation.LimitRemaining,
                    _apiRateInformation.LimitResetAt
                );
            }
            else
            {
                _apiRateInformation.MarkedStale = DateTime.Now;

                Logger.LogDebug("PVOutput API Rate: no rate info in response, ApiRateInformation is now stale (received at {Received})", _apiRateInformation.Received);
            }

            if (!response.IsSuccess)
            {
                // TODO: log
            }
        }

        private PVOutputWrapper GetApiClient(DataProviderConfiguration outputConfig)
        {
            var pvOutputConfiguration = outputConfig as PVOutputConfiguration ?? new PVOutputConfiguration(outputConfig);

            if (string.IsNullOrWhiteSpace(pvOutputConfiguration.SystemId))
                throw new ArgumentException(nameof(pvOutputConfiguration.SystemId) + " not configured.");

            if (_apiClientCache.TryGetValue(pvOutputConfiguration.SystemId, out var apiClient))
            {
                return apiClient;
            }

            Logger.LogDebug("Creating PVOutput client for system {systemId}", pvOutputConfiguration.SystemId);

            return _apiClientCache[pvOutputConfiguration.SystemId] = new PVOutputWrapper(pvOutputConfiguration);
        }

        private Task WriteJson<T>(string filePrefix, T response)
        {
            if (string.IsNullOrWhiteSpace(_jsonDataDirectory))
            {
                return Task.CompletedTask;
            }

            var json = JsonSerializer.Serialize(response, _responseLogJsonFormat);

            var jsonFileName = Path.Combine(_jsonDataDirectory, $"{filePrefix}{DateTime.Now:yyyy-MM-dd_HH_mm_ss}.json");

            return File.WriteAllTextAsync(jsonFileName, json);
        }
    }
}
