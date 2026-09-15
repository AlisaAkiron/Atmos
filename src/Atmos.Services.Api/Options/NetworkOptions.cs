namespace Atmos.Services.Api.Options;

/// <summary>
/// How this deployment sits behind a reverse proxy.
/// </summary>
public record NetworkOptions
{
    /// <summary>
    /// Honour <c>X-Forwarded-For</c>, <c>-Proto</c> and <c>-Host</c>.
    /// </summary>
    /// <remarks>
    /// Leave this off when Kestrel is reached directly. The headers are supplied by
    /// whoever made the request; only a proxy that overwrites them makes them worth
    /// believing, and trusting them without one lets any caller claim a different
    /// address and scheme.
    /// </remarks>
    public bool UseForwardedHeaders { get; set; }

    /// <summary>
    /// How many proxy hops to walk back through.
    /// </summary>
    /// <remarks>
    /// One is right for a single proxy. Raise it for a chain — a CDN in front of an
    /// ingress is two — remembering that every hop in the count is trusted to have
    /// written the truth.
    /// </remarks>
    public int ForwardLimit { get; set; } = 1;

    /// <summary>
    /// Addresses of the proxies to trust, e.g. <c>10.0.0.5</c>.
    /// </summary>
    /// <remarks>
    /// Empty, together with <see cref="KnownNetworks" />, trusts any proxy. That is
    /// the intended default for a container reachable only through its ingress, whose
    /// address is not knowable ahead of time; it is the wrong answer anywhere the app
    /// can also be reached directly.
    /// </remarks>
    public List<string> KnownProxies { get; set; } = [];

    /// <summary>
    /// CIDR ranges of the proxies to trust, e.g. <c>10.0.0.0/8</c>.
    /// </summary>
    public List<string> KnownNetworks { get; set; } = [];
}
