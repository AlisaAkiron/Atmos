using System.Text.RegularExpressions;

namespace Atmos.Common.Utils;

public static partial class MediaKeyUtils
{
    private const int MaxLength = 512;

    // Each '/'-separated segment starts and ends with an alphanumeric character
    // and may contain '.', '_', '-' in between. This forbids leading/trailing/double
    // slashes, ".."-style segments, and characters that need URL encoding.
    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?(?:/[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?)*\z")]
    private static partial Regex KeyRegex();

    public static bool IsValid(string? key)
    {
        return string.IsNullOrEmpty(key) is false
               && key.Length <= MaxLength
               && KeyRegex().IsMatch(key);
    }
}
