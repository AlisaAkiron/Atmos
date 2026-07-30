using Atmos.Services.Media.Options;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media.Services;

public class MediaUrlBuilder
{
    public MediaUrlBuilder(IOptions<MediaOptions> options)
    {
        PublicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
    }

    public string PublicBaseUrl { get; }

    public string GetPublicUrl(string key)
    {
        return $"{PublicBaseUrl}/{key}";
    }
}
