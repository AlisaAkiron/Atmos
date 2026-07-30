namespace Atmos.Services.Media.Options;

public class LocalStorageOptions
{
    /// <summary>
    /// Directory objects are stored in. Empty resolves to
    /// "{ContentRootPath}/media-storage" when media services are registered.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Request path the media directory is served at. Must match the path
    /// segment of <see cref="MediaOptions.PublicBaseUrl" />.
    /// </summary>
    public string RequestPath { get; set; } = "/media";
}
