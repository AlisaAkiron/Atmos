namespace Atmos.Services.Default.Caching;

/// <summary>
/// Shared names for the output cache. A single <c>IOutputCacheStore</c> serves the
/// whole app, so a policy picks its backing store by prefixing its cache key rather
/// than by resolving a different service.
/// </summary>
public static class AtmosOutputCache
{
    /// <summary>
    /// Policy applied to the health endpoints.
    /// </summary>
    public const string HealthChecksPolicy = "HealthChecks";

    /// <summary>
    /// Storage-key prefix that sends an entry to the process-local store instead of
    /// the shared one. Apply it with <c>OutputCachePolicyBuilder.SetCacheKeyPrefix</c>,
    /// which prepends the value verbatim to the generated storage key.
    /// </summary>
    public const string LocalKeyPrefix = "local:";
}
