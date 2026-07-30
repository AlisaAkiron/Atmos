using Atmos.Common.Utils;

namespace Atmos.Services.Media.Services;

/// <summary>
/// Finds media keys referenced by markdown content: every occurrence of
/// "{publicBaseUrl}/{key}" wherever it appears (image, link, raw HTML, bare URL).
/// A plain string scan is deliberate — it catches all markdown syntaxes at once.
/// </summary>
public static class MediaReferenceExtractor
{
    // Characters that cannot be part of a key and therefore terminate a URL in text.
    // '?' and '#' also terminate the KEY portion (query string / fragment).
    private static readonly System.Buffers.SearchValues<char> Terminators =
        System.Buffers.SearchValues.Create(" \t\r\n\"'()<>[]`?#\\");

    public static IReadOnlySet<string> ExtractKeys(string content, string publicBaseUrl)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        var prefix = publicBaseUrl.TrimEnd('/') + "/";

        var searchFrom = 0;
        while (true)
        {
            var start = content.IndexOf(prefix, searchFrom, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var keyStart = start + prefix.Length;
            var span = content.AsSpan(keyStart);
            var end = span.IndexOfAny(Terminators);
            var key = (end < 0 ? span : span[..end]).ToString();

            if (MediaKeyUtils.IsValid(key))
            {
                result.Add(key);
            }

            searchFrom = keyStart;
        }

        return result;
    }
}
