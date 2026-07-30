using Atmos.Services.Media.Enums;

namespace Atmos.Services.Media.Options;

public class MediaOptions
{
    /// <summary>Which <see cref="Abstract.IMediaStorage" /> implementation to register.</summary>
    public MediaProviderType Provider { get; set; } = MediaProviderType.Local;

    /// <summary>
    /// Absolute base URL media is publicly served from, e.g.
    /// https://atmos-resources.alisaqaq.moe. Absolute because the frontend runs
    /// on a different origin than the API.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public long MaxUploadSizeBytes { get; set; } = 104_857_600;

    public LocalStorageOptions Local { get; set; } = new();

    public S3StorageOptions S3 { get; set; } = new();
}
