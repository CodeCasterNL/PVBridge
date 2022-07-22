using Microsoft.Extensions.Configuration;

namespace CodeCaster.PVBridge.ConfigurationUI.WinForms
{
    /// <summary>
    /// No DI yet.
    /// </summary>
    internal class AppSettingsReader
    {
        public string? GetJsonDataLogDirectory()
        {
            var builder = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            var settings = configuration.GetSection("Logging.JsonData");

            var logJson = bool.TryParse(settings?.GetSection("LogJson").Value, out var doLog) && doLog;

            return logJson
                ? settings!.GetSection("DataDirectory").Value 
                : null;
        }
    }
}
