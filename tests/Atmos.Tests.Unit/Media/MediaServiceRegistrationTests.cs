using Atmos.Services.Media;
using Atmos.Services.Media.Abstract;
using Atmos.Services.Media.Options;
using Atmos.Services.Media.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Atmos.Tests.Unit.Media;

public class MediaServiceRegistrationTests
{
    private static IHost BuildHost(string provider, string localRootPath)
    {
        // Production environment keeps the container from running ValidateOnBuild,
        // which would reject MediaReferenceService (it needs AtmosDbContext)
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Media:Provider"] = provider,
            ["Media:PublicBaseUrl"] = "https://media.example.test",
            ["Media:Local:RootPath"] = localRootPath,
            ["Media:S3:ServiceUrl"] = "https://s3.example.test",
            ["Media:S3:AccessKeyId"] = "access-key",
            ["Media:S3:SecretAccessKey"] = "secret-key",
            ["Media:S3:Bucket"] = "atmos-resources"
        });

        builder.AddAtmosMediaServices();

        return builder.Build();
    }

    [Test]
    [Arguments("Local", typeof(LocalMediaStorage))]
    [Arguments("S3", typeof(S3MediaStorage))]
    public async Task Registers_The_Configured_Provider(string provider, Type expected)
    {
        var root = Path.Combine(Path.GetTempPath(), $"atmos-media-{Path.GetRandomFileName()}");

        using var host = BuildHost(provider, root);

        await Assert.That(host.Services.GetRequiredService<IMediaStorage>().GetType()).IsEqualTo(expected);
    }

    [Test]
    public async Task Empty_Local_Root_Path_Resolves_Under_The_Content_Root()
    {
        using var host = BuildHost("Local", string.Empty);

        var options = host.Services.GetRequiredService<IOptions<MediaOptions>>().Value;
        var environment = host.Services.GetRequiredService<IHostEnvironment>();

        await Assert.That(options.Local.RootPath)
            .IsEqualTo(Path.Combine(environment.ContentRootPath, "media-storage"));
    }
}
