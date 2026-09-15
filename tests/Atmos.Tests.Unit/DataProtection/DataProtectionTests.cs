using Atmos.Database;
using Atmos.Services.Api.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Atmos.Tests.Unit.DataProtection;

public class DataProtectionTests
{
    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        // A connection string is never dialled: the key ring is resolved, not read
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = "Host=postgres.example.test;Database=atmos"
        });

        builder.Services.AddDbContext<AtmosDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

        builder.ConfigureDataProtection();

        return builder.Build();
    }

    [Test]
    public async Task Keys_Are_Persisted_To_The_Database()
    {
        // The default ring is per-process and thrown away on exit, which signs every
        // session out on restart and stops two replicas reading each other's cookies
        using var host = BuildHost();

        var options = host.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        await Assert.That(options.XmlRepository)
            .IsTypeOf<EntityFrameworkCoreXmlRepository<AtmosDbContext>>();
    }

    [Test]
    public async Task The_Application_Name_Is_Pinned()
    {
        // Left unset the discriminator is the entry assembly's name, which ties the
        // ring to how the app happens to be hosted; the stored keys stop being
        // readable the moment that changes
        using var host = BuildHost();

        var options = host.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

        await Assert.That(options.ApplicationDiscriminator).IsEqualTo("Atmos");
    }

    [Test]
    public async Task The_Key_Ring_Table_Follows_The_Schema_Naming_Convention()
    {
        // DataProtectionKey comes from the framework, so it picks up none of the
        // [Table]/[Column] attributes the Atmos entities carry
        using var host = BuildHost();

        using var scope = host.Services.CreateScope();
        var entityType = scope.ServiceProvider
            .GetRequiredService<AtmosDbContext>()
            .Model
            .FindEntityType(typeof(DataProtectionKey))!;

        var columns = entityType.GetProperties().Select(x => x.GetColumnName()).ToList();

        await Assert.That(entityType.GetTableName()).IsEqualTo("data_protection_key");
        await Assert.That(columns).IsEquivalentTo(new[] { "id", "friendly_name", "xml" });
    }
}
