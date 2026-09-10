using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atmos.Database.Caching;

/// <summary>
/// Deletes expired rows from <c>atmos_cache</c>. Reads already filter on the
/// deadline, so nothing depends on this running promptly — without it the table
/// would just keep growing.
/// </summary>
internal sealed class AtmosCacheMaintenanceService : BackgroundService
{
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromMinutes(5);

    private readonly AtmosCacheStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AtmosCacheMaintenanceService> _logger;

    public AtmosCacheMaintenanceService(
        AtmosCacheStore store,
        TimeProvider timeProvider,
        ILogger<AtmosCacheMaintenanceService> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PurgeInterval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PurgeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var removed = await _store.PurgeExpiredAsync(cancellationToken);

            if (removed > 0)
            {
                _logger.LogDebug("Purged {Count} expired cache entries", removed);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // A transient database failure must not take the host down
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Failed to purge expired cache entries");
        }
    }
}
