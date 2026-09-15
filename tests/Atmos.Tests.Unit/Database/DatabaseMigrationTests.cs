using Atmos.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Atmos.Tests.Unit.Database;

public class DatabaseMigrationTests
{
    // Points at a closed port: reaching the database at all fails the test, which is
    // how "the gate short-circuited" is told apart from "the migration succeeded"
    private const string UnreachableConnectionString =
        "Host=127.0.0.1;Port=1;Database=atmos;Username=atmos;Password=atmos;Timeout=1";

    private static IHost BuildHost(bool? autoMigrate)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = UnreachableConnectionString
        };

        if (autoMigrate is not null)
        {
            settings["Database:AutoMigrate"] = autoMigrate.Value.ToString();
        }

        builder.Configuration.AddInMemoryCollection(settings);

        builder.ConfigureNpgsql();

        return builder.Build();
    }

    [Test]
    public async Task Skips_Migrating_When_AutoMigrate_Is_Disabled()
    {
        using var host = BuildHost(false);

        await Assert.That(async () => await host.MigrateAtmosDatabaseAsync()).ThrowsNothing();
    }

    [Test]
    [Arguments(true)]
    [Arguments(null)]
    public async Task Migrates_When_AutoMigrate_Is_Enabled_Or_Absent(bool? autoMigrate)
    {
        using var host = BuildHost(autoMigrate);

        // Defaulting to enabled is the point of the assertion: an unreachable database
        // can only be contacted if the gate let the migration through
        await Assert.That(async () => await host.MigrateAtmosDatabaseAsync()).ThrowsException();
    }
}
