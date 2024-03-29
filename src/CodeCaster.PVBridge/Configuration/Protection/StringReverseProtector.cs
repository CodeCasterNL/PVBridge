﻿using System.Linq;
using System.Threading.Tasks;

namespace CodeCaster.PVBridge.Configuration.Protection;

#if DEBUG
/// <summary>
/// Reversed strings in config, when prefixed with "s1_".
/// 
/// Development only.
/// </summary>
public class StringReverseProtector : IDataProtector
{
    public const string Prefix = "s1";

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

        return Prefix + new string(plainValue?.Reverse().ToArray());
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
#endif