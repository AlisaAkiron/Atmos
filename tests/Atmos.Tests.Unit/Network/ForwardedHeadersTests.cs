using System.Net;
using Atmos.Services.Api.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
// Microsoft.AspNetCore.HttpOverrides carries its own deprecated IPNetwork
using IPNetwork = System.Net.IPNetwork;

namespace Atmos.Tests.Unit.Network;

public class ForwardedHeadersTests
{
    private static ForwardedHeadersOptions Resolve(Dictionary<string, string?> configuration)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(configuration);
        builder.ConfigureNetwork();

        using var host = builder.Build();

        return host.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Test]
    public async Task Enabling_Without_A_Proxy_List_Trusts_Any_Proxy()
    {
        // The deployment shape this defaults to: a container only reachable through
        // its ingress, whose address is not knowable ahead of time
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true"
        });

        await Assert.That(options.KnownIPNetworks).IsEmpty();
        await Assert.That(options.KnownProxies).IsEmpty();
    }

    [Test]
    public async Task Leaving_It_Off_Keeps_The_Middleware_Inert()
    {
        // Kestrel reached directly: the headers are attacker-controlled, and
        // ForwardedHeaders.None is what makes the middleware ignore them
        var options = Resolve([]);

        await Assert.That(options.ForwardedHeaders).IsEqualTo(ForwardedHeaders.None);
        await Assert.That(options.KnownProxies).Contains(IPAddress.IPv6Loopback);
    }

    [Test]
    public async Task Enabling_Forwards_Address_Scheme_And_Host()
    {
        // Host matters as much as scheme: without it redirect_uri is built from the
        // internal hostname, so OIDC stays broken even once the scheme is right
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true"
        });

        await Assert.That(options.ForwardedHeaders).IsEqualTo(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);
    }

    [Test]
    public async Task Configured_Proxies_Replace_The_Loopback_Default()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true",
            ["Network:KnownProxies:0"] = "10.0.0.5",
            ["Network:KnownProxies:1"] = "2001:db8::1"
        });

        // Compared as text: structural equivalence reflects over IPAddress.ScopeId,
        // which throws for an IPv4 address
        var trusted = options.KnownProxies.Select(x => x.ToString()).ToList();

        // Replaced, not appended: an allowlist that still trusts ::1 is not the
        // allowlist that was configured
        await Assert.That(trusted).IsEquivalentTo(new[] { "10.0.0.5", "2001:db8::1" });
    }

    [Test]
    public async Task Configured_Networks_Are_Parsed_As_Cidr()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true",
            ["Network:KnownNetworks:0"] = "10.0.0.0/8"
        });

        var trusted = options.KnownIPNetworks.Select(x => x.ToString()).ToList();

        await Assert.That(trusted).IsEquivalentTo(new[] { "10.0.0.0/8" });
    }

    [Test]
    public async Task Forward_Limit_Defaults_To_A_Single_Hop()
    {
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true"
        });

        await Assert.That(options.ForwardLimit).IsEqualTo(1);
    }

    [Test]
    public async Task Forward_Limit_Can_Be_Raised_For_A_Proxy_Chain()
    {
        // e.g. CDN in front of an ingress in front of the app
        var options = Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true",
            ["Network:ForwardLimit"] = "2"
        });

        await Assert.That(options.ForwardLimit).IsEqualTo(2);
    }

    [Test]
    public async Task A_Malformed_Proxy_Address_Fails_At_Startup()
    {
        // Silently dropping it would leave the headers ignored at runtime, which
        // looks exactly like not having configured the middleware at all
        await Assert.That(() => Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true",
            ["Network:KnownProxies:0"] = "not-an-address"
        })).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task A_Malformed_Network_Range_Fails_At_Startup()
    {
        await Assert.That(() => Resolve(new Dictionary<string, string?>
        {
            ["Network:UseForwardedHeaders"] = "true",
            ["Network:KnownNetworks:0"] = "10.0.0.0/ninety"
        })).Throws<InvalidOperationException>();
    }
}
