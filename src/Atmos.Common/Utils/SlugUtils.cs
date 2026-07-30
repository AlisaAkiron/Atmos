using System.Text.RegularExpressions;

namespace Atmos.Common.Utils;

public static partial class SlugUtils
{
    private const int MaxLength = 128;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

    public static bool IsValid(string? slug)
    {
        return string.IsNullOrEmpty(slug) is false
               && slug.Length <= MaxLength
               && SlugRegex().IsMatch(slug);
    }
}
