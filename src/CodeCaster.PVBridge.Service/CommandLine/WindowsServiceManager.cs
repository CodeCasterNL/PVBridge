using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CodeCaster.PVBridge.Service.CommandLine
{
    internal class WindowsServiceManager
    {
        private readonly ILogger<WindowsServiceManager> _logger;

        // Windows Service restart timers after errors.
        private const int FirstRestartSeconds = 60;
        private const int SecondRestartSeconds = 60;
        private const int SuccessiveRestartSeconds = 86400;

        /// <summary>
        /// Event Log name.
        /// </summary>
        private const string ApplicationLogName = "Application";

        // TODO: validate setter
        private string ServiceName { get; } = "PVBridge";

        const string ServiceDisplayName = "PVBridge Solar Status Syncer";
        const string ServiceUser = @"NT AUTHORITY\Network Service";

        public WindowsServiceManager(ILogger<WindowsServiceManager> logger, string? serviceName = null)
        {
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                ServiceName = serviceName;
            }
        }

        internal void StopService(ServiceController serviceController)
        {
            if (serviceController.Status == ServiceControllerStatus.Stopped)
            {
                _logger.LogInformation("Service {serviceName} stop requested, but not running", ServiceName);

                return;
            }

            _logger.LogInformation("Service {serviceName} stop requested, stopping", ServiceName);

            serviceController.Stop(stopDependentServices: true);

            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);

            _logger.LogDebug("Service {serviceName} stopped", ServiceName);
        }

        internal async Task InstallServiceAsync(IServiceProvider services, string servicePath, CancellationToken token)
        {
            if (!File.Exists(servicePath))
            {
                _logger.LogWarning("Service installation requested, but path \"{servicePath}\" not found", servicePath);

                throw new InvalidOperationException($"The service path \"{servicePath}\" does not point to a valid executable file.");
            }

            // Create the event log source so we can log warnings and up to the event log.
            try
            {
                if (!EventLog.SourceExists(Program.ApplicationName))
                {
                    _logger.LogInformation("Creating {application} log source {source}", ApplicationLogName, Program.ApplicationName);

                    EventLog.CreateEventSource(Program.ApplicationName, ApplicationLogName);
                }
            }
            catch (Exception e)
            {
                // That's not a fatal error.
                _logger.LogError(e, "Creating {application} log source {source} failed", ApplicationLogName, Program.ApplicationName);
            }


            using (var serviceController = GetServiceController())
            {
                if (serviceController == null)
                {
                    throw new InvalidOperationException($"Service controller for {ServiceName} could not be obtained.");
                }

                // Might be an update. Just reinstall, maybe we moved paths?
                if (IsServiceInstalled(ServiceName))
                {
                    StopService(serviceController);

                    await UninstallServiceAsync(services, token);
                }
            }

            var exitCode = await InstallServiceAsync(servicePath);

            if (exitCode != 0)
            {
                // User canceled or process failed, report that to the installer.
                throw new InvalidOperationException($"Service {ServiceName} could not be installed. Exit code: {exitCode}.");
            }

            // Need to reload after installation.
            using (var serviceController = GetServiceController())
            {
                if (serviceController == null)
                {
                    throw new InvalidOperationException($"Service {ServiceName} was installed, but a service controller could not be obtained.");
                }

                serviceController.Refresh();

                serviceController.Start();

                var serviceStartTimeout = TimeSpan.FromSeconds(30);

                serviceController.WaitForStatus(ServiceControllerStatus.Running, serviceStartTimeout);

                serviceController.Refresh();

                if (serviceController.Status != ServiceControllerStatus.Running)
                {
                    _logger.LogWarning("Service \"{serviceName}\" installed, but failed to start in {serviceStartTimeout}.", ServiceName, serviceStartTimeout);

                    throw new InvalidOperationException($"Service {ServiceName} was installed, but failed to start in {Math.Ceiling(serviceStartTimeout.TotalSeconds)} seconds.");
                }
            }

            _logger.LogInformation("Service \"{serviceName}\" installed and started successfully.", ServiceName);
        }

        internal async Task UninstallServiceAsync(IServiceProvider services, CancellationToken token)
        {
            _logger.LogInformation("Service \"{serviceName}\" uninstallation requested", ServiceName);

            if (!IsServiceInstalled(ServiceName))
            {
                _logger.LogWarning("Service {serviceName} installation requested, but was not installed", ServiceName);

                // But return success, so the installer knows it can proceed.
                return;
            }

            var serviceController = GetServiceController();
            if (serviceController == null)
            {
                throw new InvalidOperationException($"Service controller for \"{ServiceName}\" could not be obtained.");
            }

            StopService(serviceController);

            var exitCode = await UninstallServiceAsync();

            switch (exitCode)
            {
                case 0:
                    return;
                // Access denied.
                case 5:
                    throw new InvalidOperationException($"The service \"{ServiceName}\" could not be uninstalled. Exit code: {exitCode}. Run as administrator.");
                // User canceled or process failed, report that to the installer.
                default:
                    throw new InvalidOperationException($"The service \"{ServiceName}\" could not be uninstalled. Exit code: {exitCode}.");
            }
        }

        private ServiceController? GetServiceController()
        {
            try
            {
                _logger.LogInformation("Service controller requested for {ServiceName}", ServiceName);

                return new ServiceController(ServiceName);
            }
            catch (InvalidOperationException e) when (e.InnerException is Win32Exception { NativeErrorCode: 5 })
            {
                _logger.LogWarning("Service start requested as non-administrator");

                Console.WriteLine("Access Denied. Run as Administrator.");

            }
            catch (InvalidOperationException e) when (e.InnerException is Win32Exception { NativeErrorCode: 1060 })
            {
                _logger.LogWarning("Service status requested while service isn't installed");

                Console.WriteLine("Service is not installed.");
            }

            return null;
        }

        private async Task<int> InstallServiceAsync(string servicePath)
        {
            var binPath = $"{servicePath} service run";

            var scArguments = $"create {ServiceName} displayName= \"{ServiceDisplayName}\" start= auto depend= RpcSs binPath= \"{binPath}\" obj= \"{ServiceUser}\"";

            var exitCode = await RunCreateServiceAsync(scArguments);

            if (exitCode != 0)
            {
                return exitCode;
            }

            // Set failure mode: retry twice after 2 minutes, then after a day. Error count resets in a day.
            string restartActionString = $"restart/{FirstRestartSeconds * 1000}/restart/{SecondRestartSeconds * 1000}/restart/{SuccessiveRestartSeconds * 1000}";
            
            scArguments = $"failure {ServiceName} reset= 86400 actions= {restartActionString}";

            return await RunCreateServiceAsync(scArguments);
        }

        private Task<int> UninstallServiceAsync()
        {
            _logger.LogInformation("Uninstalling service {serviceName}", ServiceName);

            var scArguments = $"delete {ServiceName}";

            return RunCreateServiceAsync(scArguments);
        }

        // TODO: P/Invoke CreateServiceW?
        private async Task<int> RunCreateServiceAsync(string arguments)
        {
            _logger.LogDebug("Running sc.exe {arguments}", arguments);

            var processInfo = new ProcessStartInfo("sc.exe")
            {
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            try
            {
                var process = Process.Start(processInfo);

                if (process == null)
                {
                    _logger.LogError("sc.exe process null");

                    Console.WriteLine("Could not create sc.exe process.");

                    return -1;
                }

                var standardOutput = await process.StandardOutput.ReadToEndAsync();
                var standardError = await process.StandardError.ReadToEndAsync();

                _logger.LogDebug("sc.exe output: {output}", standardOutput);

                if (!string.IsNullOrWhiteSpace(standardError))
                {
                    _logger.LogWarning("sc.exe error: {error}", standardError);
                }

                await process.WaitForExitAsync();

                var exitCode = process.ExitCode;

                if (exitCode != 0)
                {
                    _logger.LogWarning("sc.exe exit code: {exitCode}", exitCode);
                }
                else
                {
                    _logger.LogInformation("Service creation command executed sucessfully");
                }

                return exitCode;
            }
            catch (Win32Exception e) when (e.NativeErrorCode == 1223)
            {
                _logger.LogError("User canceled elevation");

                Console.WriteLine("Administrative permissions not granted, cannot install service.");

                return -2;
            }
        }

        private static bool IsServiceInstalled(string serviceName) => ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
    }
}
