using System.Linq;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection;

#if DEBUG
/// <summary>
/// Plaintext strings in config.
/// 
/// Development only.
/// </summary>
public class UnprotectedProtector : IDataProtector
{
    public const string Prefix = "s2_";

    public Task InitializeAsync(bool forWriting)
    {
        return Task.CompletedTask;
    }

    public string? Protect(string? plainValue)
    {
        if (string.IsNullOrEmpty(plainValue))
        {
            return null;
        }

        return Prefix + plainValue;
    }

    public string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return null;
        }

        return protectedValue;
    }
}
#endif