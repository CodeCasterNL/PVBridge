using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection
{
    /// <summary>
    /// To prevent having a file with plaintext credentials readable by programs running under a Windows user.
    ///
    /// Because we don't necessarily touch each property when saving the file, prepend the algorithm to the value. Algorithms can determine their own attributes to store in the string.
    /// </summary>
    public static class ConfigurationProtector
    {
        private static IDataProtector GetBestProtector()
        {
            //#if DEBUG
            //            return DataProtectors[StringReverseProtector.Prefix];
            //#endif

            return DataProtectors[AesProtector.Prefix];
        }

        private static readonly Dictionary<string, IDataProtector> DataProtectors;

        static ConfigurationProtector()
        {
            DataProtectors = new Dictionary<string, IDataProtector>()
            {
                { AesProtector.Prefix, new AesProtector() },
                
#if DEBUG
                { StringReverseProtector.Prefix, new StringReverseProtector() },
                { UnprotectedProtector.Prefix, new UnprotectedProtector() },
#endif

            };
        }

        public static async Task ProtectAsync(BridgeConfiguration loadedConfiguration)
        {
            foreach (var provider in loadedConfiguration.Providers)
            {
                await ProtectAsync(provider);
            }
        }

        public static async Task UnprotectAsync(BridgeConfiguration loadedConfiguration)
        {
            foreach (var provider in loadedConfiguration.Providers)
            {
                await UnprotectAsync(provider);
            }
        }

        // TODO: pass JSON path to have useful error messages
        private static (IDataProtector?, string?) GetProtector(string? protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue))
            {
                return (null, null);
            }

            if (protectedValue.Length < 3)
            {
                throw new ArgumentException($"Invalid protected value '{protectedValue}'");
            }

            var algorithm = protectedValue[..3];
            var remainder = protectedValue[3..];

            if (!DataProtectors.TryGetValue(algorithm, out var dataProtector))
            {
                throw new ArgumentException($"Invalid algorithm '{algorithm}'");
            }
            
            return (dataProtector, remainder);
        }

        private static async Task ProtectAsync(DataProviderConfiguration provider)
        {
            // Skip when already protected.
            if (provider.IsProtected)
            {
                return;
            }

            await InitializeDataProtectorsAsync(forWriting: true);

            provider.Account = Protect(provider.Account);
            provider.Key = Protect(provider.Key);

            foreach (var (key, value) in provider.Options)
            {
                provider.Options[key] = Protect(value);
            }

            provider.IsProtected = true;
        }

        private static async Task UnprotectAsync(DataProviderConfiguration provider)
        {
            // Skip when already unprotected.
            if (!provider.IsProtected)
            {
                return;
            }

            await InitializeDataProtectorsAsync(forWriting: false);

            provider.Account = Unprotect(provider.Account);
            provider.Key = Unprotect(provider.Key);

            foreach (var (key, value) in provider.Options)
            {
                provider.Options[key] = Unprotect(value);
            }

            provider.IsProtected = false;
        }

        private static string? Protect(string? protectedValue)
        {
            var protector = GetBestProtector();

            return protector.Protect(protectedValue);
        }

        private static string? Unprotect(string? protectedValue)
        {
            var (protector, remainingValue) = GetProtector(protectedValue);

            if (protector == null || remainingValue == null)
            {
                return null;
            }

            return protector.Unprotect(remainingValue);
        }

        private static async Task InitializeDataProtectorsAsync(bool forWriting)
        {
            foreach (var dataProtector in DataProtectors.Values)
            {
                await dataProtector.InitializeAsync(forWriting);
            }
        }
    }
}
