using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.GoodWe;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;
using CodeCaster.PVBridge.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCaster.PVBridge.GoodWe
{
    /// <summary>
    /// Wrapper class to convert calls to GoodWe.
    ///
    /// Very rudimentary.
    /// </summary>
    public class GoodWeApiClient : CachingSummaryProvider, IInputProvider
    {
        private readonly Dictionary<string, GoodWeClient> _apiClientCache = new CaseInsensitiveDictionary<GoodWeClient>();
        private readonly string? _jsonDataDirectory;

        public GoodWeApiClient(ILogger<GoodWeApiClient> logger, IOptions<LoggingConfiguration> loggingConfig, IClientMessageBroker messageBroker)
            : base("GoodWe", logger, messageBroker)
        {
            _jsonDataDirectory = loggingConfig.Value.JsonData?.CreateAndGetDataDirectory();
        }

        public async Task<ApiResponse<Snapshot>> GetCurrentStatusAsync(DataProviderConfiguration configuration, CancellationToken cancellationToken)
        {
            var goodWeConfig = GetConfig(configuration);

            var plantId = GetConfigOrThrow(goodWeConfig.PlantId, nameof(goodWeConfig.PlantId));

            var apiClient = GetApiClient(goodWeConfig);

            Logger.LogDebug("Getting GoodWe current status for system {plantId}", plantId);

            var snapshot = await apiClient.GetCurrentStatus(plantId, cancellationToken);

            var mapped = Mapper.Map(Logger, snapshot.Data);

            return HandleResponse(snapshot, mapped);
        }

        protected override async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetDaySummariesAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var goodWeConfig = GetConfig(configuration);

            var plantId = GetConfigOrThrow(goodWeConfig.PlantId, nameof(goodWeConfig.PlantId));

            var apiClient = GetApiClient(goodWeConfig);

            Logger.LogDebug("Getting GoodWe summaries from {since} until {until} for system {plantId}", since, until, plantId);

            var outputs = await apiClient.GetSummariesAsync(plantId, since, until, cancellationToken);

            var mapped = Mapper.Map(outputs.Data);

            // API data seems to have gaps in its summaries sometimes, but they do catch up. Usually.
            //if (day.DailyGeneration.GetValueOrDefault() == 0)
            //{
            //if (d == 0 && m == 0)
            //{
            //    // We haven't received data before for this backlog sync.
            //    _logger.LogWarning("No input summary data for {input} on {day}, probably API connectivity errors, backing off", _inputProvider.NameOrType, day.LoggableDayName());
            //
            //    _taskStatus.HandleApiResponse(ApiResponse.RateLimited(DateTime.Now.AddMinutes(15)));
            //
            //    return;
            //}
            //}

            return HandleResponse(outputs, mapped);
        }

        private static string GetConfigOrThrow(string? value, string name)
            => string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException($"configuration.{name}", $"Configuration parameter '{name}' is required for this operation.")
                : value;

        public async Task<ApiResponse<IReadOnlyCollection<Snapshot>>> GetSnapshotsAsync(DataProviderConfiguration configuration, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            var goodWeConfig = GetConfig(configuration);

            var plantId = GetConfigOrThrow(goodWeConfig.PlantId, nameof(goodWeConfig.PlantId));
            var inverterSerialNumber = GetConfigOrThrow(goodWeConfig.InverterSerialNumber, nameof(goodWeConfig.InverterSerialNumber));

            var apiClient = GetApiClient(goodWeConfig);
            var snapshots = await apiClient.GetDayDetailsAsync(plantId, inverterSerialNumber, since, until, cancellationToken);
            var mapped = Mapper.Map(Logger, snapshots.Data);

            return HandleResponse(snapshots, mapped);
        }

        private static ApiResponse<TOut> HandleResponse<TOut>(GoodWeApiResponse response, TOut? data)
        {
            // TODO: actually handle response

            return data == null
                ? new ApiResponse<TOut>(ApiResponseStatus.Failed)
                : new ApiResponse<TOut>(data);
        }

        private GoodWeClient GetApiClient(GoodWeInputConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.Account))
            {
                throw new ArgumentNullException(nameof(configuration), $"Configuration parameter '{nameof(configuration.Account)}' is required for this operation.");
            }

            if (_apiClientCache.TryGetValue(configuration.Account, out var apiClient))
            {
                return apiClient;
            }

            var accountConfig = new AccountConfiguration(configuration.Account, configuration.Key);

            return _apiClientCache[configuration.Account] = new GoodWeClient(Logger, _jsonDataDirectory, accountConfig);
        }

        private static GoodWeInputConfiguration GetConfig(DataProviderConfiguration configuration)
        {
            GoodWeInputConfiguration config = new(configuration);

            return config;
        }
    }
}
