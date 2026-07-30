using Atmos.Services.Media.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media.Services;

/// <summary>
/// Creates the local media root at startup so a missing directory fails the host
/// loudly instead of 500-ing on every upload. Mirrors <see cref="BucketInitializer" />
/// for the Local provider.
/// </summary>
public class MediaDirectoryInitializer : IHostedService
{
    private readonly LocalStorageOptions _options;

    public MediaDirectoryInitializer(IOptions<MediaOptions> options)
    {
        _options = options.Value.Local;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.RootPath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
