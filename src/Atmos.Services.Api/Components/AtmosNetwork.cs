using System.Net;
using Atmos.Common.Extensions;
using Atmos.Services.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// Microsoft.AspNetCore.HttpOverrides carries its own IPNetwork, deprecated in .NET 10
// in favour of this one (ASPDEPR005)
using IPNetwork = System.Net.IPNetwork;

namespace Atmos.Services.Api.Components;

public static class AtmosNetwork
{
    /// <summary>
    /// Configures the forwarded-headers middleware from the <c>Network</c> section.
    /// </summary>
    /// <remarks>
    /// Off by default, so running Kestrel directly is unaffected. Behind a
    /// TLS-terminating proxy it is what lets the app see the request the client
    /// actually made: without it the scheme reads as <c>http</c>, which builds an
    /// OIDC <c>redirect_uri</c> the provider rejects and suppresses every
    /// <c>Secure</c> cookie, and the client address reads as the proxy's, which
    /// collapses per-IP rate limiting onto a single partition.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A configured proxy address or network range is malformed.
    /// </exception>
    public static IHostApplicationBuilder ConfigureNetwork(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<NetworkOptions>(builder.Configuration.GetSection("Network"));

        var network = builder.Configuration.GetOptions<NetworkOptions>("Network");

        if (network.UseForwardedHeaders is false)
        {
            return builder;
        }

        // Parsed here rather than inside the callback so a malformed value fails the
        // host at startup. Deferred to first resolution it would surface as a request
        // that ignored the headers, which looks exactly like never having configured
        // the middleware at all.
        var knownProxies = network.KnownProxies.Select(ParseProxy).ToList();
        var knownNetworks = network.KnownNetworks.Select(ParseNetwork).ToList();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                       | ForwardedHeaders.XForwardedProto
                                       | ForwardedHeaders.XForwardedHost;

            options.ForwardLimit = network.ForwardLimit;

            // Cleared rather than added to: the defaults trust loopback, which no
            // containerised proxy ever is, and an allowlist that still trusts ::1
            // is not the allowlist that was configured. Leaving both empty is the
            // documented way to trust any proxy.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var knownNetwork in knownNetworks)
            {
                options.KnownIPNetworks.Add(knownNetwork);
            }
        });

        return builder;
    }

    private static IPAddress ParseProxy(string value)
    {
        return IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException(
                $"Network:KnownProxies contains '{value}', which is not a valid IP address.");
    }

    private static IPNetwork ParseNetwork(string value)
    {
        return IPNetwork.TryParse(value, out var network)
            ? network
            : throw new InvalidOperationException(
                $"Network:KnownNetworks contains '{value}', which is not a valid CIDR range.");
    }
}
