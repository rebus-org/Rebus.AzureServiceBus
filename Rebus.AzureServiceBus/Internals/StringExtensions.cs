using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Internals;

static class StringExtensions
{
    public static string TrimTo(this string str, int maxLength)
    {
        if (str == null) return null;

        if (str.Length < maxLength) return str;

        const string ellipsis = " (...)";

        return string.Concat(str.Substring(0, maxLength - ellipsis.Length), ellipsis);
    }

    public static Dictionary<string, string> ParseKeyValuePairs(this string str, StringComparer comparer = null)
    {
        if (str == null) return new();

        return str.Split(';')
            .Select(part => part.Trim())
            .Select(part => part.Split('=').Select(s => s.Trim()).ToArray())
            .Where(parts => parts.Length == 2)
            .Select(parts => new
            {
                Key = parts[0],
                Value = parts[1]
            })
            .ToDictionary(a => a.Key, a => a.Value, comparer ?? StringComparer.Ordinal);
    }
}