using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Output;

namespace CodeCaster.PVBridge.CsvWriter
{
    public class CsvOutputWriter : IOutputWriter
    {
        public string Type => "Csv";

        public Task<ApiResponse> WriteStatusAsync(DataProviderConfiguration outputConfig, Snapshot snapshot, CancellationToken _)
        {
            // TODO
            return Task.FromResult(ApiResponse.Succeeded);
        }

        public async Task<ApiResponse> WriteStatusesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<Snapshot> snapshots, CancellationToken cancellationToken)
        {
            // TODO: get path from options
            var day = snapshots.First().TimeTaken;

            var filename = $"C:\\Temp\\{day.Year}-{day.Month}-{day.Day} Snapshots.csv";

            await using var writer = new StreamWriter(filename);
            await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

            await csv.WriteRecordsAsync(snapshots.Select(Mapper.Map), cancellationToken);

            return ApiResponse.Succeeded;
        }

        public async Task<ApiResponse<IReadOnlyCollection<DaySummary>>> WriteDaySummariesAsync(DataProviderConfiguration outputConfig, IReadOnlyCollection<DaySummary> summaries, CancellationToken cancellationToken)
        {
            var groupedByMonth = summaries.GroupBy(d => (d.Day.Year, d.Day.Month));

            foreach (var monthData in groupedByMonth)
            {
                await WriteMonthAsync(outputConfig, monthData, cancellationToken);
            }

            // TODO
            return new ApiResponse<IReadOnlyCollection<DaySummary>>(ApiResponseStatus.Succeeded);
        }

        private static async Task WriteMonthAsync(DataProviderConfiguration outputConfig, IGrouping<(int Year, int Month), DaySummary> monthData, CancellationToken cancellationToken)
        {
            // TODO: get path from options
            var filename = $"C:\\Temp\\{monthData.Key.Year}-{monthData.Key.Month} Report.csv";

            await using var writer = new StreamWriter(filename);
            await using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

            // TODO: read, merge, write.
            await csv.WriteRecordsAsync(monthData.Select(Mapper.Map), cancellationToken);
        }

        public bool CanWriteDetails(System.DateTime day)
        {
            return true;
        }

        public Task<ApiResponse<IReadOnlyCollection<DaySummary>>> GetSummariesAsync(DataProviderConfiguration outputConfig, System.DateTime since, System.DateTime? until = null, CancellationToken cancellationToken = default)
        {
            // TODO: read the files that we have, get last record for each day, cache
            return Task.FromResult(new ApiResponse<IReadOnlyCollection<DaySummary>>(new List<DaySummary>()));
        }

        // unless disk full but let it throw
        public bool CanWriteDetails(DataProviderConfiguration outputConfig, DateTime day) => true;
        public bool CanWriteSummary(DataProviderConfiguration outputConfig, DateTime day) => true;
    }
}
