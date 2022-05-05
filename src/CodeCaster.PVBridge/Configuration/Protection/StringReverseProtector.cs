using System.Linq;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection;

/// <summary>
/// Development only.
/// </summary>
public class StringReverseProtector : IDataProtector
{
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

        return "s1_" + new string(plainValue?.Reverse().ToArray());
    }

    public string? Unprotect(string? protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return null;
        }

        return new string(protectedValue?.Reverse().ToArray());
    }
}
