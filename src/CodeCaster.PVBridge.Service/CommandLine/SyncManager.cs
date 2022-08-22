using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Utils;

namespace CodeCaster.PVBridge.Service.CommandLine
{
    internal class SyncManager
    {
        internal async Task SyncAsync(IInputToOutputWriter ioWriter, DataProviderConfiguration inputConfig, DataProviderConfiguration outputConfig, DateTime? since, DateTime? until, int snapshotDays, int sleep, CancellationToken cancellationToken)
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

            // TODO: get an InputToOutputLoop (refactor backlog sync) or we're gonna rebuild the whole thing here.

            var now = DateTime.Now;

            until ??= new[] { now, since.Value }.Min();

            if (since > until || until > now)
            {
                Console.WriteLine("The since date should be before the until date, and the latter before now. Type --help for more information.");

                return;
            }


            if (snapshotDays <= 0)
            {
                snapshotDays = 14;
            }
            
            if (sleep <= 0)
            {
                sleep = 5;
            }

            // TODO: summaryDays 90 (?) for PVOutput

            var inputSummaries = await ioWriter.GetSummariesAsync(inputConfig, since.Value, until.Value, cancellationToken);
            if (inputSummaries.Status != ApiResponseStatus.Succeeded || inputSummaries.Response == null)
            {
                Console.WriteLine("Error getting input summaries: " + inputSummaries.Status);

                return;
            }

            var outputSummaries = await ioWriter.GetSummariesAsync(outputConfig, since.Value.Date, until.Value.Date, cancellationToken);
            if (outputSummaries.Status != ApiResponseStatus.Succeeded || outputSummaries.Response == null)
            {
                Console.WriteLine("Error getting output summaries: " + outputSummaries.Status);

                return;
            }

            var sleepTimeSpan = TimeSpan.FromSeconds(sleep);

            foreach (var inputSummary in inputSummaries.Response)
            {
                bool dayTooOld = (now.Date - inputSummary.Day).TotalDays > snapshotDays;
                if (dayTooOld && !ioWriter.CanWriteDetails(outputConfig, inputSummary.Day))
                {
                    Console.WriteLine($"Can't write snapshots for {inputSummary.Day.LoggableDayName()} to {outputConfig.NameOrType}");
                }
                else
                {
                    if (dayTooOld)
                    {
                        Console.WriteLine($"Data too old for {inputSummary.Day.LoggableDayName()} to {outputConfig.NameOrType}, still trying...");
                    }

                    var response = await ioWriter.SyncPeriodDetailsAsync(inputConfig, outputConfig, inputSummary.Day, force: !dayTooOld, cancellationToken);

                    if (response.Status != ApiResponseStatus.Succeeded)
                    {
                        Console.WriteLine("Error writing day details for " + inputSummary.Day.LoggableDayName() + ": " + response.Status);

                        if (response.RetryAfter.HasValue)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Retry after: " + response.RetryAfter.Value.ToString("O"));
                        }

                        return;
                    }
                }

                if (!ioWriter.CanWriteSummary(outputConfig, inputSummary.Day))
                {
                    Console.WriteLine($"Can't write summary for {inputSummary.Day.LoggableDayName()} to {outputConfig.NameOrType}");
                }
                else
                {
                    // Same problem in InputToOutputLoop.
                    var outputSummary = outputSummaries.Response.FirstOrDefault(d => d.Day == inputSummary.Day);

                    var summaryResponse = await ioWriter.WriteDaySummaryAsync(outputConfig, inputSummary, outputSummary, cancellationToken);

                    if (summaryResponse.Status != ApiResponseStatus.Succeeded)
                    {
                        Console.WriteLine("Failed to write summary for " + inputSummary.Day.LoggableDayName() + ": " + summaryResponse.Status);

                        return;
                    }
                }

                if (inputSummary == inputSummaries.Response.Last())
                {
                    return;
                }

                Console.Write("Sleeping: " + sleepTimeSpan + "... ");
                await Task.Delay(sleepTimeSpan, cancellationToken);
                Console.WriteLine();
                Console.WriteLine();
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
