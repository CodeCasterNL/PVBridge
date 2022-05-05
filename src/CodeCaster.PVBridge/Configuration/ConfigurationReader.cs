using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration
{
    public static class ConfigurationReader
    {
        public static string GlobalSettingsFilePath => 
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PVBridge", 
                "PVBridge.AccountConfig.json");

        /// <summary>
        /// TODO: more error handling
        /// </summary>
        public static async Task<BridgeConfiguration?> ReadFromSystemAsync()
        {
            var json = await File.ReadAllTextAsync(GlobalSettingsFilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            var rootConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);

            if (rootConfig?.TryGetValue(BridgeConfiguration.SectionName, out var section) != true)
            {
                return null;
            }

            return section.Deserialize<BridgeConfiguration>(options);
        }
    }
}
