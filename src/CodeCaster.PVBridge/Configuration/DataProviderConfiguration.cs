using System;
using System.Linq;
using System.Text.Json.Serialization;
using CodeCaster.PVBridge.Utils;

#pragma warning disable 8618 // nullable checks and instantiation are done by the configuration system
// ReSharper disable PropertyCanBeMadeInitOnly.Global, AutoPropertyCanBeMadeGetOnly.Global, MemberCanBeProtected.Global - config class, remove when covered by tests
namespace CodeCaster.PVBridge.Configuration
{
    /// <summary>
    /// Reads <see cref="IInputProvider"/> and <see cref="IOutputWriter"/> configuration.
    /// </summary>
    public class DataProviderConfiguration
    {
        /// <summary>
        /// Whether the configuration system has protected this provider's values.
        /// </summary>
        [JsonIgnore]
        public bool IsProtected { get; set; } = true;

        /// <summary>
        /// Optional name to configure more of one type.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The type name of the input or output. Currently supported: "GoodWe" as input, "PVOutput" as output. 
        /// </summary>
        /// <devdoc>
        /// Debugging: GoodWeFileReader, CSV.
        /// </devdoc>
        public string Type { get; set; }

        public DateTime? GetOptionDateTime(string key) => Options.TryGetValue(key, out var s) && DateTime.TryParse(s, out var d) ? d : null;

        /// <summary>
        /// Either the name or the type.
        /// </summary>
        [JsonIgnore]
        public string NameOrType => Name ?? Type;

        /// <summary>
        /// For display purposes.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Account name or email address, if any.
        /// </summary>
        public string? Account { get; set; }

        /// <summary>
        /// API key, password, ...
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Case-insensitive implementation-specific properties. See usage in GoodWeInputConfiguration, PVOutputConfiguration.
        /// </summary>
        public CaseInsensitiveDictionary<string?> Options { get; set; } = new();

        public DataProviderConfiguration() { }

        public DataProviderConfiguration(DataProviderConfiguration other)
            : this()
        {
            Type = other.Type;
            Name = other.Name;
            Description = other.Description;

            Account = other.Account;
            Key = other.Key;

            // Copy so you can't update loaded configs by reference.
            Options = new CaseInsensitiveDictionary<string?>(other.Options);

            IsProtected = other.IsProtected;
        }

        public bool Equals(DataProviderConfiguration? other)
        {
            return other is not null
                && Type == other.Type
                && Name == other.Name
                && NameOrType == other.NameOrType
                && Account == other.Account
                && Key == other.Key
                && Options.Count == other.Options.Count && !Options.Except(other.Options).Any();
        }
    }
}
#pragma warning restore 8618
