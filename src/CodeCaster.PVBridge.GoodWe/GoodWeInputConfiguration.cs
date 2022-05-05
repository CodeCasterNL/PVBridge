using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CodeCaster.PVBridge.Configuration;

namespace CodeCaster.PVBridge.GoodWe
{
    public class GoodWeInputConfiguration : DataProviderConfiguration
    {
        public GoodWeInputConfiguration()
        {
            Type = "GoodWe";
        }

        public GoodWeInputConfiguration(DataProviderConfiguration configuration) : base(configuration) { }

        [JsonIgnore]
        public string? PlantId
        {
            get => Options.GetValueOrDefault(nameof(PlantId));
            set => Options[nameof(PlantId)] = value;
        }

        /// <summary>
        /// Required for generating reports to get detailed day data.
        /// </summary>
        [JsonIgnore]
        public string? InverterSerialNumber
        {
            get => Options.GetValueOrDefault(nameof(InverterSerialNumber));
            set => Options[nameof(InverterSerialNumber)] = value;
        }

        /// <summary>
        /// System turn on date (first data received by GoodWe?).
        /// </summary>
        [JsonIgnore]
        public DateTime? InstallDate
        {
            get
            {
                var installDate = Options.GetValueOrDefault(nameof(InstallDate));
                return DateTime.TryParse(installDate, out var datetime) ? datetime : null;
            }
            set => Options[nameof(InstallDate)] = value?.ToString("O");
        }
    }
}
