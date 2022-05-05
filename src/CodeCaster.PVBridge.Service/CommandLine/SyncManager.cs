using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;

namespace CodeCaster.PVBridge.Service.CommandLine
{
    internal class SyncManager
    {
        internal async Task SyncAsync(IInputToOutputWriter ioWriter, DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime? since, DateTime? until, int sleep, CancellationToken cancellationToken)
        {
            if (since == null)
            {
                if (until != null)
                {
                    // TODO: complain
                }

                await SyncLiveStatusAsync(ioWriter, inputConfig, outputConfig, cancellationToken);

                return;
            }

            until ??= new[] { DateTime.Now, since.Value.Date.AddDays(1) }.Min();

            var inputSummaries = await ioWriter.GetSummariesAsync(inputConfig, since.Value, until.Value, cancellationToken);
            if (inputSummaries.Status != ApiResponseStatus.Succeeded || inputSummaries.Response == null)
            {
                Console.WriteLine("Error getting input summaries: " + inputSummaries.Status);

                return;
            }

            var outputSummaries = await ioWriter.GetSummariesAsync(inputConfig, since.Value, until.Value, cancellationToken);
            if (outputSummaries.Status != ApiResponseStatus.Succeeded || outputSummaries.Response == null)
            {
                Console.WriteLine("Error getting output summaries: " + outputSummaries.Status);

                return;
            }

            var sleepTimeSpan = TimeSpan.FromSeconds(sleep);

            foreach (var inputSummary in inputSummaries.Response)
            {
                if (inputSummary.Day < since.Value || inputSummary.Day > until.Value)
                {
                    continue;
                }

                var response = await ioWriter.SyncPeriodDetailsAsync(inputConfig, outputConfig, inputSummary.Day, cancellationToken);

                if (response.Status != ApiResponseStatus.Succeeded)
                {
                    Console.WriteLine("Error writing day details: " + response.Status);

                    if (response.RetryAfter.HasValue)
                    {
                        Console.WriteLine("Retry after: " + response.RetryAfter.Value.ToString("O"));
                    }

                    return;
                }

                var outputSummary = outputSummaries.Response.FirstOrDefault(d => d.Day == inputSummary.Day);

                var summaryResponse = await ioWriter.WriteDaySummaryAsync(outputConfig, inputSummary, outputSummary, cancellationToken);

                if (summaryResponse.Status != ApiResponseStatus.Succeeded)
                {
                    Console.WriteLine("Failed to write summary: " + summaryResponse.Status);

                    return;
                }

                Console.WriteLine("sleeping: " + sleepTimeSpan);

                await Task.Delay(sleepTimeSpan, cancellationToken);
            }
        }

        private static async Task SyncLiveStatusAsync(IInputToOutputWriter writer, DataProviderConfiguration input, DataProviderConfiguration output, CancellationToken cancellationToken)
        {
            var currentStatus = await writer.GetLiveSnapshotAsync(input, cancellationToken);
            if (currentStatus.Status != ApiResponseStatus.Succeeded || currentStatus.Response == null)
            {
                Console.WriteLine("Error reading status: " + currentStatus.Status);
                return;
            }

            if (currentStatus.Response.TimeTaken.Date < DateTime.Now.Date)
            {
                Console.WriteLine("This status is old news: " + currentStatus.Response.TimeTaken);
                return;
            }

            var syncResponse = await writer.WriteSnapshotAsync(input.NameOrType, output, currentStatus.Response, cancellationToken);
            if (syncResponse.Status != ApiResponseStatus.Succeeded)
            {
                Console.WriteLine("Error writing status: " + currentStatus.Status);
            }
        }
    }
}
