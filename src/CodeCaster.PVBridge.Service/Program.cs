using System;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Configuration;
using CodeCaster.PVBridge.Service.CommandLine;
using CodeCaster.PVBridge.Service.Grpc;
using CodeCaster.WindowsServiceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCaster.PVBridge.Service
{
    /// <summary>
    /// Entry point for console app and Windows Service, as well as Service (un)install.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Event Log source name.
        /// </summary>
        public static readonly string ApplicationName = "PVBridge Service";

        public static async Task Main(string[] args)
        {
            try
            {
                // Required for installer. Service does start from own directory.
                Environment.CurrentDirectory = AppContext.BaseDirectory;

                var host = CreateHostBuilder(args).Build();

                var rootCommand = new RootCommand();

                // PVbridge.exe service <install --path xxx |uninstall|run|start>
                rootCommand.AddCommand(GetServiceCommand(host));

                // PVbridge.exe sync [yyyy-MM-dd [yyyy-MM-dd]] [--input xxx] [--output xxx] [--snapshotDays 90] [--sleep 3]
                rootCommand.AddCommand(GetSyncCommand(host));

                await rootCommand.InvokeAsync(args);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(e);

                if (Environment.UserInteractive)
                {
                    Pause();
                }

                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Builds the commandline command to sync (PVBridge.exe sync 2022-07-23).
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private static Command GetSyncCommand(IHost host)
        {
            var sinceArgument = new Argument<DateTime?>("since", "The day to sync, or start syncing from. Format: yyyy-MM-dd.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            var untilArgument = new Argument<DateTime?>("until", "The inclusive end date to sync. Format: yyyy-MM-dd.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };

            // TODO: require when multiple found in config
            var inputOption = new Option<string>(new[] { "--input", "-i" }, "Select input by name.")
            {
                IsRequired = false
            };

            // TODO: require when multiple found in config
            var outputOption = new Option<string>(new[] { "--output", "-o" }, "Select output by name.")
            {
                IsRequired = false
            };

            var snapshotDaysOption = new Option<int>(
                new[] { "--snapshot-days", "-d" },
                () => 14,
                "Number of days to sync snapshots back. When syncing further back, only summaries will be written. Defaults to 14.")
            {
                IsRequired = false,
            };

            // TODO: validate sane values.
            var sleepOption = new Option<int>(
                new[] { "--sleep", "-s" },
                () => 5,
                "Seconds to wait between each day, defaults to 5")
            {
                IsRequired = false
            };

            var syncCommand = new Command("sync", "Without further arguments, syncs the current status. When one date is passed that day is synced, when two the range between them.")
            {
                sinceArgument,
                untilArgument,
                inputOption,
                snapshotDaysOption,
                outputOption,
                sleepOption,
            };

            syncCommand.SetHandler(async (DateTime? since, DateTime? until, string? input, string? output, int snapshotDays, int sleep, CancellationToken token) =>
            {
                // Sync the current status if since and until are null.
                if (since == null && until.HasValue)
                {
                    Console.WriteLine("You cannot pass until without since. Type --help for more information.");

                    return;
                }

                var config = host.Services.GetRequiredService<IOptions<BridgeConfiguration>>();

                var configProvider = new ProviderProvider(config);
                var (inputConfig, outputConfig) = await configProvider.GetConfigurationAsync(input, output);

                if (inputConfig == null)
                {
                    Console.WriteLine($"Input provider '{input}' not found.");

                    return;
                }

                if (outputConfig == null)
                {
                    Console.WriteLine($"Output provider '{output}' not found.");

                    return;
                }

                var writer = host.Services.GetRequiredService<IInputToOutputWriter>();

                await new SyncManager().SyncAsync(writer, inputConfig, outputConfig, since, until, snapshotDays, sleep, token);
            },
                sinceArgument,
                untilArgument,
                inputOption,
                outputOption,
                snapshotDaysOption,
                sleepOption
            );

            return syncCommand;
        }

        /// <summary>
        /// Builds the commandline command to run and (un)install the service.
        /// </summary>
        private static Command GetServiceCommand(IHost host)
        {
            var serviceManager = new WindowsServiceManager(host.Services.GetRequiredService<ILogger<WindowsServiceManager>>());

            return new Command("service", "Windows Service commands")
            {
                GetRunCommand(),
                GetInstallCommand(),
                GetUninstallCommand(),
            };

            Command GetRunCommand()
            {
                var command = new Command("run", "Run the service");

                // The PvBridgeService reads the command line arguments by itself, we can't pass the idle option there,
                // but we do need to specify it on the command here to provide validation and help.
                var idleOption = new Option<bool>(new[] { "--idle", "-i" }, "Run as service, with an idle main loop: no API/file input or output apart from logging. Listens for gRPC calls.");

                command.AddOption(idleOption);

                command.SetHandler(async (CancellationToken token) =>
                {
                    await host.RunAsync(token);
                });

                return command;
            }

            Command GetInstallCommand()
            {
                var installCommand = new Command("install", "Install the service");

                var pathOption = new Option<string>("--path")
                {
                    Description = "Path to the service executable",
                    IsRequired = true,
                };

                installCommand.AddOption(pathOption);

                installCommand.SetHandler(async (string path, CancellationToken token) =>
                {
                    await serviceManager.InstallServiceAsync(host.Services, path, token);
                }, pathOption);

                return installCommand;
            }

            Command GetUninstallCommand()
            {
                var uninstallCommand = new Command("uninstall", "Uninstall the service");
                uninstallCommand.SetHandler(async (CancellationToken token) =>
                {
                    await serviceManager.UninstallServiceAsync(host.Services, token);
                });

                return uninstallCommand;
            }
        }

        private static void Pause()
        {
            Console.Write("Press any key to continue... ");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            SetCultureInfo();

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Sets the Event Log source name (through UseWindowsServiceExtensions -> UseWindowsService).
                    context.HostingEnvironment.ApplicationName = Program.ApplicationName;

                    // Read "C:\ProgramData\PVBridge\PVBridge.AccountConfig.json". Optional because it doesn't exist on first run.
                    config.AddJsonFile(ConfigurationReader.GlobalSettingsFilePath, optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    DependencyInjection.ConfigureServices(services, context.Configuration);

                    // The main service.
                    services.AddHostedService<PvBridgeService>();

                    // The gRPC service. TODO: Scrutor.
                    services.AddSingleton<MessageBroker>();
                    services.AddSingleton<IClientMessageBroker, MessageBroker>(r => r.GetRequiredService<MessageBroker>());
                    services.AddSingleton<IClientAndServiceMessageBroker, MessageBroker>(r => r.GetRequiredService<MessageBroker>());

                    //services.AddHostedService<GrpcService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddSimpleConsole(console =>
                    {
                        console.IncludeScopes = true;
                        console.SingleLine = true;
                        console.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    });
                })
                .UseWindowsServiceExtensions();
        }

        /// <summary>
        /// Set culture for logging.
        ///
        /// TODO: but doesn't work anymore.
        /// </summary>
        private static void SetCultureInfo()
        {
            var cultureInfo = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            cultureInfo.NumberFormat.CurrencySymbol = "€";

            // DateTime.ToString() "combines the custom format strings returned by the ShortDatePattern and LongTimePattern"
            cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            cultureInfo.DateTimeFormat.LongTimePattern = "HH:mm:ss";

            cultureInfo.DateTimeFormat.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }

        private static async Task HandleExceptionAsync(Exception e)
        {
            Console.WriteLine("PVBridge Service command failed: " + e.Message);

            var errorLog = DateTimeOffset.Now.ToString("O") + ": Exception: " + e;
            var logFileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".PVBridge.Service.crash.log");

            await File.WriteAllTextAsync(logFileName, errorLog);

            Console.WriteLine($"See {logFileName} for error details.");
        }
    }
}
