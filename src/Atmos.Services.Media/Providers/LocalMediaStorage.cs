using Atmos.Common.Utils;
using Atmos.Services.Media.Abstract;
using Atmos.Services.Media.Options;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media.Providers;

/// <summary>
/// Filesystem-backed storage: objects live at "{RootPath}/{key}" and are served
/// over HTTP by the media static-file middleware. Single-instance only — two API
/// replicas need a shared volume or the S3 provider.
/// </summary>
public class LocalMediaStorage : IMediaStorage
{
    private readonly string _rootPath;

    public LocalMediaStorage(IOptions<MediaOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.Local.RootPath);
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        // Not persisted: the static-file middleware derives Content-Type from the
        // extension, which is what MediaEndpoints.ResolveContentType prefers too
        _ = contentType;

        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Same-directory sibling so the move below stays within one volume
        var tempPath = $"{path}.tmp-{Path.GetRandomFileName()}";

        try
        {
            await using (var file = File.Create(tempPath))
            {
                await content.CopyToAsync(file, ct);
            }

            // Atomic rename: the object is publicly readable the instant it lands
            // under its final name, so it must never be observable half-written
            File.Move(tempPath, path, true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // The original failure (e.g. disk full) is the interesting one;
                // don't let a secondary failure to clean up the temp file mask it.
            }

            throw;
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = ResolvePath(key);

        // Absent objects are not an error, matching S3's idempotent delete.
        // The Exists guard also avoids DirectoryNotFoundException when the
        // key's prefix directory was never created.
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(ResolvePath(key)));
    }

    private string ResolvePath(string key)
    {
        if (MediaKeyUtils.IsValid(key) is false)
        {
            throw new ArgumentException($"Invalid media key '{key}'", nameof(key));
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, key));

        var rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        // Redundant given MediaKeyUtils, kept because the two checks fail independently
        if (path.StartsWith(rootPrefix, StringComparison.Ordinal) is false)
        {
            throw new ArgumentException($"Media key '{key}' resolves outside the media root", nameof(key));
        }

        return path;
    }
}
