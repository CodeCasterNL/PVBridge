using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeCaster.PVBridge.GoodWe.TestConsole
{
    internal static class Program
    {
        private static async Task Main()
        {
            var config = ReadAccountFromConsole();
            config.Options["PlantId"] = ReadStationIdFromConsole();

            var client = new GoodWeApiClient(NullLogger<GoodWeApiClient>.Instance, Options.Create(new LoggingConfiguration()), null!);

            do
            {
                var data = await client.GetCurrentStatusAsync(config, CancellationToken.None);
                if (data.Response == null)
                {
                    Console.WriteLine(DateTime.Now + ": no data received, login error?");
                    return;
                }

                Console.WriteLine(DateTime.Now + ": refresh time: " + data.Response.TimeTaken + ", power: " + data.Response.ActualPower);

                await Task.Delay(TimeSpan.FromMinutes(2));
            }
            while (true);
        }

        private static string ReadStationIdFromConsole()
        {
            const string defaultStationId = "";
            Console.Write($"StationId [{defaultStationId}]: ");
            var stationId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(stationId))
            {
                stationId = defaultStationId;
            }

            return stationId;
        }

        private static DataProviderConfiguration ReadAccountFromConsole()
        {
            const string defaultAccount = "";
            Console.Write($"Account [{defaultAccount}]: ");
            var account = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(account))
            {
                account = defaultAccount;
            }

            var password = new StringBuilder();
            Console.Write("Password: ");
            do
            {
                var c = Console.ReadKey(intercept: true);
                if (c.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (c.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                }
                else
                {
                    password.Append(c.KeyChar);
                }
            }
            while (true);
            Console.WriteLine();

            var accountConfig = new GoodWeInputConfiguration
            {
                Account = account,
                Key = password.ToString(),
                IsProtected = false,
            };

            return accountConfig;
        }
    }
}
