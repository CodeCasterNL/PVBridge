using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection;

/// <summary>
/// Protects configuration values from prying eyes.
/// </summary>
public interface IDataProtector
{
    /// <summary>
    /// Called before reading or writing config, so we can check or generate keys or whatever.
    /// </summary>
    Task InitializeAsync(bool forWriting);

    string? Protect(string? plainValue);

    string? Unprotect(string? protectedValue);
}
