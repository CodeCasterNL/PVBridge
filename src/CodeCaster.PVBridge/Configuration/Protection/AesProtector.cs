using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection;

#pragma warning disable CA1416 // Validate platform compatibility - we're a Windows Service
/// <summary>
/// AES, save key, IV, in admin/service account readable file.
/// </summary>
public class AesProtector : IDataProtector
{
    private KeyConfiguration? _keyConfig;

    // ReSharper disable once InconsistentNaming
    public record KeyConfiguration(byte[] Key, byte[] IV, string? Nonce);

    public async Task InitializeAsync(bool forWriting)
    {
        var configFile = ConfigurationReader.GlobalSettingsFilePath;

        var keyFile = Path.Combine(
            Path.GetDirectoryName(configFile)!,
            Path.GetFileNameWithoutExtension(configFile) + ".aes.json"
        );

        if (forWriting)
        {
            await TryReadConfigAsync(keyFile);
            EnsurePermissions(keyFile);
        }

        _keyConfig = await TryReadConfigAsync(keyFile);

        if (_keyConfig == null && forWriting)
        {
            _keyConfig = await GenerateAndWriteKeyConfigurationFileAsync(keyFile);
        }
    }

    /// <summary>
    /// Makes the key file only readable by Administrators and Network Service.
    /// </summary>
    /// <param name="keyFile"></param>
    private void EnsurePermissions(string keyFile)
    {
        var fileInfo = new FileInfo(keyFile);
        if (!fileInfo.Exists)
        {
            fileInfo.Create().Dispose();
        }

        var accessControl = fileInfo.GetAccessControl();

        var administratorsIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var networkServiceIdentifier = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);

        var adminRule = new FileSystemAccessRule(administratorsIdentifier, FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
        var networkRule = new FileSystemAccessRule(networkServiceIdentifier, FileSystemRights.Read, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);

        accessControl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        accessControl.SetOwner(administratorsIdentifier);
        
        accessControl.AddAccessRule(adminRule);
        accessControl.AddAccessRule(networkRule);

        fileInfo.SetAccessControl(accessControl);
    }

    /// <summary>
    /// Make the key file readable by Adminstrators and Network Service.
    /// </summary>
    private async Task<KeyConfiguration> GenerateAndWriteKeyConfigurationFileAsync(string keyFile)
    {
        using Aes myAes = Aes.Create();

        var config = new KeyConfiguration(myAes.Key, myAes.IV, null);
        
        await using var configStream = File.OpenWrite(keyFile);

        await JsonSerializer.SerializeAsync(configStream, config);

        return config;
    }

    private static async Task<KeyConfiguration?> TryReadConfigAsync(string keyFile)
    {
        try
        {
            if (!File.Exists(keyFile))
            {
                return null;
            }

            await using var stream = File.OpenRead(keyFile);

            var config = await JsonSerializer.DeserializeAsync<KeyConfiguration>(stream);

            return config;
        }
        catch (Exception e)
        {
            // TODO: log

            return null;
        }
    }

    public string? Protect(string? plainValue)
    {
        if (_keyConfig?.IV == null || _keyConfig.Key == null)
        {
            throw new InvalidOperationException("Uninitialized");
        }

        if (string.IsNullOrEmpty(plainValue))
        {
            return null;
        }

        return "a1_" + Convert.ToBase64String(EncryptStringToBytes_Aes(plainValue, _keyConfig.Key, _keyConfig.IV));
    }

    public string? Unprotect(string? protectedValue)
    {
        if (_keyConfig?.IV == null || _keyConfig.Key == null)
        {
            throw new InvalidOperationException("Uninitialized");
        }

        if (string.IsNullOrEmpty(protectedValue))
        {
            return null;
        }

        return DecryptStringFromBytes_Aes(Convert.FromBase64String(protectedValue), _keyConfig.Key, _keyConfig.IV);
    }

    // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-6.0
    static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        // Check arguments.
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException(nameof(plainText));
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException(nameof(Key));
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException(nameof(IV));

        // Create an Aes object
        // with the specified key and IV.
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = Key;
        aesAlg.IV = IV;

        // Create an encryptor to perform the stream transform.
        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        // Create the streams used for encryption.
        using MemoryStream msEncrypt = new MemoryStream();
        using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        {
            //Write all data to the stream.
            swEncrypt.Write(plainText);
        }

        // Return the encrypted bytes from the memory stream.
        return msEncrypt.ToArray();
    }

    static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] IV)
    {
        // Check arguments.
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException(nameof(cipherText));
        if (key == null || key.Length <= 0)
            throw new ArgumentNullException(nameof(key));
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException(nameof(IV));

        // Create an Aes object
        // with the specified key and IV.
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = IV;

        // Create a decryptor to perform the stream transform.
        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        // Create the streams used for decryption.
        using MemoryStream msDecrypt = new MemoryStream(cipherText);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        
        // Read the decrypted bytes from the decrypting stream
        // and place them in a string.
        return srDecrypt.ReadToEnd();
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
