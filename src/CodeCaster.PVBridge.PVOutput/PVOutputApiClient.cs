using CodeCaster.PVBridge.Utils;
using PVOutput.Net.Builders;
using PVOutput.Net.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task<PVOutputSystem?> GetSystemAsync(PVOutputConfiguration configuration, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(configuration);

            var systemResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.System.GetOwnSystemAsync(cancellationToken), $"PVOutput-GetOwnSystem({apiClient.ApiClient.OwnedSystemId}-TODO-logparams)");

            var system = systemResponse.Response?.Value;

            return system == null ? null : new PVOutputSystem(apiClient.ApiClient.OwnedSystemId, system);
        }

        public async Task<ApiResponse> WriteStatusAsync(DataProviderConfiguration configuration, Snapshot snapshot, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(configuration);

            Logger.LogTrace("Mapping snapshot: {snapshot}", snapshot);
            var status = Mapper.Map(_statusBuilder, snapshot);

            var addStatusResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Status.AddStatusAsync(status, cancellationToken), "PVOutput-AddStatus");

            return addStatusResponse;
        }

        public async Task<ApiResponse> WriteDaySummariesAsync(DataProviderConfiguration configuration, IReadOnlyCollection<DaySummary> summaries, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(configuration);

            // We can only send output (summary) data up till yesterday.
            var items = summaries.Where(s => s.Day < DateTime.Now)
                                 .Select(s =>
                                 {
                                     Logger.LogTrace("Mapping summary: {summary}", s);
                                     return Mapper.Map(_batchOutputBuilder, s);
                                 })
                                 .ToArray();

            Logger.LogDebug("Syncing {summaryCount} to PVOutput for system {systemId}", items.Length.SIfPlural("symmary", "summaries"), apiClient.ApiClient.OwnedSystemId);

            ApiResponse<PVOutputBasicResponse> addOutputResponse = ApiResponse<PVOutputBasicResponse>.Failed();

            foreach (var outputPostBatch in items.Batch(BatchSize))
            {
                foreach (var outputPost in outputPostBatch)
                {
                    addOutputResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.AddOutputAsync(outputPost, cancellationToken), "PVOutput-Output-AddBatch");

                    if (!addOutputResponse.IsSuccessful)
                    {
                        return addOutputResponse;
                    }
                }
            }

            return addOutputResponse;
        }

        public async Task<ApiResponse> WriteStatusesAsync(DataProviderConfiguration configuration, IReadOnlyCollection<Snapshot> snapshots, CancellationToken cancellationToken)
        {
            var apiClient = GetApiClient(configuration);

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

                batchResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Status.AddBatchStatusAsync(items, cancellationToken), "PVOutput-Status-AddBatch");

                if (batchResponse.Status != ApiResponseStatus.Succeeded)
                {
                    return batchResponse;
                }
            }

            return batchResponse;
        }

        protected override async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var summaries = new List<DaySummary>();

            // PVOutput can't get summaries for the current day.
            if (until == null || until.Value.Date >= DateTime.Today)
            {
                until = DateTime.Today.AddDays(-1);
            }

            // Backlog sync for today requested.
            if (until <= since)
            {
                return summaries;
            }

            var apiClient = GetApiClient(configuration);

            Logger.LogDebug("Getting PVOutput summaries from {since} until {until} for system {systemId}", since, until, apiClient.ApiClient.OwnedSystemId);

            var summariesResponse = await TryHandleAndLogRequest(() => apiClient.ApiClient.Output.GetOutputsForPeriodAsync(since, until.Value, cancellationToken: cancellationToken), "PVOutput-Output-GetOutputsForPeriod");

            if (summariesResponse.Status == ApiResponseStatus.RateLimited)
            {
                return ApiResponse<IReadOnlyCollection<DaySummary>>.RateLimited(summariesResponse.RetryAfter!.Value);
            }

            if (summariesResponse.Response?.Values == null)
            {
                Logger.LogDebug("Empty response");

                return summaries.AsReadOnly();
            }

            summaries.AddRange(summariesResponse.Response.Values.Select(s => new DaySummary
            {
                Day = s.OutputDate,
                DailyGeneration = s.EnergyGenerated,
            }));

            return summaries;
        }

        public bool CanWriteDetails(DateTime day) => day <= DateTime.Now && day >= DateTime.Today.AddDays(-13);

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

        private PVOutputWrapper GetApiClient(DataProviderConfiguration configuration)
        {
            var pvOutputConfiguration = configuration as PVOutputConfiguration ?? new PVOutputConfiguration(configuration);
            
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
