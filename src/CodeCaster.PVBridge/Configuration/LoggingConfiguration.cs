using System;
using System.IO;

#pragma warning disable 8618 // nullable checks and instantiation are done by the configuration system (right?)
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace CodeCaster.PVBridge.Configuration
{
    /// <summary>
    /// Contains configuration data for _what_ and _where_ to log. Log4Net config is in its own XML file, log4net.config.
    /// </summary>
    public class LoggingConfiguration
    {
        public const string SectionName = "Logging";
        public const string Log4NetSectionName = "Log4Net";        
        
        /// <summary>
        /// Configures whether and where to log JSON input and output, such as API traffic.
        /// </summary>
        public Jsondata? JsonData { get; set; }
    }

    public class Jsondata
    {
        public bool LogJson { get; set; }
        public string DataDirectory { get; set; }

        /// <summary>
        /// Returns the absolute path to the JSON-data-logging-directory, if configuration specifies that we should log. It also ensures the directory exists.
        /// </summary>
        /// <returns></returns>
        public string? CreateAndGetDataDirectory()
        {
            if (!LogJson)
            {
                return null;
            }

            // Make relative path relative to application, or return absolute path from config
            string jsonDataDirectory = Path.IsPathRooted(DataDirectory) 
                    ? DataDirectory 
                    : Path.Combine(AppContext.BaseDirectory, DataDirectory);

            Directory.CreateDirectory(jsonDataDirectory);

            return jsonDataDirectory;
        }
    }
}
#pragma warning restore 8618
