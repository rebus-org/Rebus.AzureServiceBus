using System.Linq;

namespace Rebus.Internals
{
    static class StringExtensions
    {
        public static string TrimTo(this string str, int maxLength)
        {
            if (str == null) return null;

            if (str.Length < maxLength) return str;

            const string ellipsis = " (...)";

            return string.Concat(str.Substring(0, maxLength - ellipsis.Length), ellipsis);
        }
    }
}