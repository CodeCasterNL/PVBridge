using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.GoodWe.Json;
using Microsoft.Extensions.Logging;

namespace CodeCaster.GoodWe
{
    public class GoodWeClient
    {
        private readonly AccountConfiguration _accountConfiguration;
        private const string DefaultApiRoot = "https://semsportal.com";
        private string _apiRoot = DefaultApiRoot;

        private const string GetMonitorDetailEndpoint = "/api/v1/PowerStation/GetMonitorDetailByPowerstationId";
        private const string GetStatisticsDataEndpoint = "/api/v1/Statistics/GetStatisticsData";
        private const string GetHistoryDataChartEndpoint = "/api/v1/HistoryData/GetStationHistoryDataChart";
        private const string GetPowerStationInfoEndpoint = "/api/v1/HistoryData/QueryPowerStationByHistory";

        private const string LoginEndpoint = "/api/v1/Common/CrossLogin";
        private const string DefaultToken = @"{""version"":""v2.0.4"",""client"":""ios"",""language"":""en""}";

        private const string DateFormatSettingsEndpoint = "/api/v2/Common/GetDateFormatSettingList";

        private string _token = DefaultToken;
        private readonly JsonSerializerOptions _responseLogJsonFormat = new() { WriteIndented = true };
        private JsonSerializerOptions? _serializerOptions;

        // TODO: HttpClientFactory injection, and do we need to dispose/recreate on .NET 6 or not (DNS changes)?
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly string? _jsonDataDirectory;

        public GoodWeClient(ILogger logger, string? jsonDataDirectory, AccountConfiguration accountConfiguration)
        {
            if (string.IsNullOrWhiteSpace(accountConfiguration.Account))
                throw new ArgumentException(nameof(accountConfiguration.Account) + " not configured.");
            if (string.IsNullOrWhiteSpace(accountConfiguration.Key))
                throw new ArgumentException(nameof(accountConfiguration.Key) + " not configured.");

            _logger = logger;
            _jsonDataDirectory = jsonDataDirectory;
            _accountConfiguration = accountConfiguration;
            _client = CreateClient();
        }

        public async Task<GoodWeApiResponse<ReportData>> GetBatchAsync(string plantId, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            string endpoint = _apiRoot + GetStatisticsDataEndpoint;

            since = new DateTime(since.Year, since.Month, since.Day, 0, 0, 0, DateTimeKind.Utc);
            until = until == null
                ? null
                : new DateTime(until.Value.Year, until.Value.Month, until.Value.Day, 0, 0, 0, DateTimeKind.Utc);

            var request = new ReportRequest(plantId, since, until)
            {
                // What do these do?
                Range = 1,
                Type = 2,

                // TODO: properly support paging
                PageIndex = 1,
                PageSize = 40
            };

            _logger.LogDebug("Getting statistics data with parameters {request}", JsonSerializer.SerializeToDocument(request).RootElement.ToString());

            var response = await TryRequest<ReportData>(() => _client.PostAsJsonAsync(endpoint, request, _serializerOptions, cancellationToken), cancellationToken);

            await WriteJson("ReportData", response);

            // TODO: error handling
            return new GoodWeApiResponse<ReportData>(response);
        }

        public async Task<GoodWeApiResponse<ChartData>> GetHistoryDataAsync(string plantId, string inverterSerialNumber, DateTime since, DateTime? until, CancellationToken cancellationToken)
        {
            string endpoint = _apiRoot + GetHistoryDataChartEndpoint;

            until ??= since.AddDays(1);

            var request = new ChartDataRequest
            {
                qry_time_start = since.ToString("yyyy'-'MM'-'dd HH:mm"),
                qry_time_end = until.Value.ToString("yyyy'-'MM'-'dd HH:mm"),
                times = 5,
                qry_status = 1,
                pws_historys = new[]
                {
                    new Pws_Historys
                    {
                        id = plantId,
                        status = -1,
                        inverters = new []
                        {
                            new Pws_Historys_Inverter
                            {
                                sn = inverterSerialNumber,
                            }
                        }
                    }
                },
                targets = new[]
                {
                    // TODO: query QueryTargetByEquipmentType whether this device supports these?

                    // Daily Generation(kWh)
                    new ChartDataRequestTarget("EDay"),

                    // Power(W)
                    new ChartDataRequestTarget("Pac"),

                    // Temperature(℃)
                    new ChartDataRequestTarget("Tempperature"),

                    // Ua(V)    
                    new ChartDataRequestTarget("Vac1"),
                }
            };

            var response = await TryRequest<ChartData>(() => _client.PostAsJsonAsync(endpoint, request, cancellationToken), cancellationToken);

            await WriteJson("StationHistoryData", response);

            // TODO: error handling
            return new GoodWeApiResponse<ChartData>(response);
        }

        public async Task<GoodWeApiResponse<PowerStationMonitorData>> GetMonitorDetailRaw(string plantId, CancellationToken cancellationToken)
        {
            string endpoint = _apiRoot + GetMonitorDetailEndpoint;

            var request = new PowerStationMonitorRequest(plantId);

            var response = await TryRequest<PowerStationMonitorData>(() => _client.PostAsJsonAsync(endpoint, request, cancellationToken), cancellationToken);

            await WriteJson("PowerStationMonitorData", response);

            // TODO: error handling
            return new GoodWeApiResponse<PowerStationMonitorData>(response);
        }

        public async Task<GoodWeApiResponse<InverterData>> GetPlantListAsync(CancellationToken cancellationToken)
        {
            string endpoint = _apiRoot + GetPowerStationInfoEndpoint;

            var request = new PowerStationListRequest(pageIndex: 1);

            var response = await TryRequest<InverterData>(() => _client.PostAsJsonAsync(endpoint, request, cancellationToken), cancellationToken);

            await WriteJson("GetInverterListAsync", response);

            // TODO: error handling
            return new GoodWeApiResponse<InverterData>(response);
        }

        [DebuggerStepThrough]
        private async Task<TResponse?> TryRequest<TResponse>(Func<Task<HttpResponseMessage>> call, CancellationToken cancellationToken)
            where TResponse : class
        {
            async Task<ResponseBase<TResponse?>?> GetResponse()
            {
                try
                {
                    var response = await call();
                    var localResponseObject = await response.Content.ReadFromJsonAsync<ResponseBase<TResponse?>?>(_serializerOptions, cancellationToken: cancellationToken);
                    return localResponseObject;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancellation is requested, fail fast
                    _logger.LogDebug("Cancellation requested, cancelling request");

                    throw;
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif

                    _logger.LogError(ex, "TryRequest() failed: ");
                    return default;
                }
            }

            _logger.LogDebug("Calling GoodWe API");

            var responseObject = await GetResponse();

            // 100001: "No access, please login."
            // 100002: "The authorization has expired, please login again."
            if (responseObject?.Code is "100001" or "100002")
            {
                if (responseObject.Code == "100001")
                {
                    _logger.LogDebug("Unauthorized, logging in");
                }
                else if (responseObject.Code == "100002")
                {
                    _logger.LogDebug("Token expired, logging in again");
                }

                if (!await LoginAsync())
                {
                    _logger.LogWarning("Login failed.");
                    return default;
                }

                await ReadUserDateFormatAsync();

                // Try again after logging in, letting it fail if that fails.
                responseObject = await GetResponse();
            }

            if (responseObject?.Code != "0")
            {
                var jsonString = responseObject != null ? JsonSerializer.Serialize(responseObject) : "null";

                _logger.LogWarning("Unexpected response: {jsonString}", jsonString);

                return default;
            }

            return responseObject.Data;
        }

        private async Task<bool> LoginAsync()
        {
            // Take first and last character...
            var logSafeAccount = _accountConfiguration.Account![..1];
            logSafeAccount += "...@...";
            logSafeAccount += _accountConfiguration.Account.Substring(_accountConfiguration.Account.Length - 1, 1);

            _logger.LogDebug("Logging in {logSafeAccount}", logSafeAccount);

            string endpoint = _apiRoot + LoginEndpoint;
            var response = await _client.PostAsJsonAsync(endpoint, new LoginRequest(_accountConfiguration.Account, _accountConfiguration.Key!));
            var responseObject = await response.Content.ReadFromJsonAsync<ResponseBase<LoginResponse>>();

            //"code": 100005
            //"msg": "Email or password error."
            if (responseObject?.Code == "100005")
            {
                // TODO: handle login errors
                _logger.LogError("Login failed: {code}: {msg}", responseObject.Code, responseObject.Msg);

                return false;
            }
            if (responseObject?.Code != "0")
            {
                _logger.LogError("Unexpected response: {response}", responseObject != null ? JsonSerializer.Serialize(responseObject) : "null");

                return false;
            }

            var token = responseObject.Data;
            _token = JsonSerializer.Serialize(token);

            if (string.IsNullOrWhiteSpace(token.token) || string.IsNullOrWhiteSpace(token.uid))
            {
                _logger.LogError("Token.token or UID are empty");

                return false;
            }

            //_apiRoot = responseObject.Components.MsgSocketAdr ?? DefaultApiRoot;
            _client.DefaultRequestHeaders.Remove("Token");
            _client.DefaultRequestHeaders.Add("Token", _token);

            var logSafeToken = _token.Replace(token.token, "***")
                                     .Replace(token.uid, "***");

            _logger.LogDebug("Logged in: {logSafeToken}", logSafeToken);

            return true;
        }

        /// <summary>
        /// API output date format depends on user UI settings.
        /// </summary>
        private async Task ReadUserDateFormatAsync()
        {
            _logger.LogDebug("Reading date settings");

            string endpoint = _apiRoot + DateFormatSettingsEndpoint;

            var dateSettingsResponse = await _client.PostAsync(endpoint, null);
            var responseObject = await dateSettingsResponse.Content.ReadFromJsonAsync<ResponseBase<DateFormatSettingsList>>();

            var selected = responseObject.Data.DateFormats.FirstOrDefault(f => f.isselected);

            _logger.LogDebug("Translating date format {selectedDateFormat}", selected.date_text);

            var format = _dateFormats[selected.date_text];

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new DateTimeConverter(format),
                    new NullableDateTimeConverter(format),
                }
            };

            _logger.LogDebug("Dates will be deserialized using format {format}", format);
        }

        /// <summary>
        /// Map user settings to .NET formats.
        /// </summary>
        private readonly Dictionary<string, string> _dateFormats = new()
        {
            { "dateYmd1", "yyyy'/'MM'/'dd"},
            { "dateYMD", "yyyy'.'MM'.'dd"},
            { "dateYmd2", "yy'/'M'/'d"},
            { "dateDmy1", "dd'/'MM'/'yyyy"},
            { "dateDMY", "dd'.'MM'.'yyyy"},
            { "dateMdy1", "MM'/'dd'/'yyyy"},
            { "dateMDY", "MM'.'dd'.'yyyy"},
            { "dateYDM", "yyyy'/'dd'/'MM"},
            { "dateDmy2", "d'/'M'/'yy"},
            { "dateMdy2", "M'/'d'/'yy"},
            { "dateYdm1", "yy'/'d'/'M"}
        };

        // TODO: IHttpClientFactory injection
        private HttpClient CreateClient()
        {
            var client = new HttpClient();

            // Make sure you change this to something valid, when you do.
            client.DefaultRequestHeaders.Add("User-Agent", "PVMaster/2.0.4 (iPhone; iOS 11.4.1; Scale/2.00)");

            client.DefaultRequestHeaders.Add("Token", _token);

            return client;
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
