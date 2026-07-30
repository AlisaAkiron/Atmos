namespace Atmos.Services.Media.Enums;

public enum MediaProviderType
{
    /// <summary>Objects on the local filesystem, served by the media static-file middleware.</summary>
    Local,

    /// <summary>Objects in an S3-compatible bucket (Cloudflare R2 in production).</summary>
    S3
}
