namespace Atmos.Services.Media.Options;

public class S3StorageOptions
{
    /// <summary>S3-compatible endpoint, e.g. https://&lt;account-id&gt;.r2.cloudflarestorage.com or the RustFS endpoint in dev.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "atmos-resources";

    /// <summary>Dev-only: create the bucket and set an anonymous-read policy on startup (RustFS).</summary>
    public bool EnsureBucketOnStartup { get; set; }
}
