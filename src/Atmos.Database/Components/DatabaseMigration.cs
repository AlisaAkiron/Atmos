using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atmos.Database;

public static class DatabaseMigration
{
    internal const string ActivitySourceName = "Migrations";

    private const string AutoMigrateKey = "Database:AutoMigrate";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>
    /// Creates the database if it does not exist yet and applies every pending migration.
    /// Call this between <c>Build()</c> and <c>RunAsync()</c> so the schema is in place
    /// before the host starts accepting requests.
    /// </summary>
    /// <remarks>
    /// Does nothing when <c>Database:AutoMigrate</c> is <c>false</c>, which leaves the schema
    /// to a separate deployment step (<c>dotnet ef database update</c> or a generated script)
    /// running under a credential allowed to alter it.
    /// </remarks>
    public static async Task<IHost> MigrateAtmosDatabaseAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (configuration.GetValue(AutoMigrateKey, true) is false)
        {
            return host;
        }

        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AtmosDbContext>();

            await EnsureDatabaseAsync(dbContext, cancellationToken);
            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        return host;
    }

    private static async Task EnsureDatabaseAsync(AtmosDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(true,
            async (context, state, ct) =>
            {
                var dbCreator = context.GetService<IRelationalDatabaseCreator>();

                if (await dbCreator.ExistsAsync(ct) is false)
                {
                    await dbCreator.CreateAsync(ct);
                }

                return state;
            },
            async (context, _, ct) =>
            {
                var dbCreator = context.GetService<IRelationalDatabaseCreator>();
                var exists = await dbCreator.ExistsAsync(ct);

                return new ExecutionResult<bool>(exists, exists);
            }, cancellationToken);

        if (result is false)
        {
            throw new InvalidOperationException("Failed to ensure database is created.");
        }
    }

    private static async Task RunMigrationAsync(AtmosDbContext dbContext, CancellationToken cancellationToken)
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any() is false)
        {
            return;
        }

        // MigrateAsync manages its own transactions; wrapping it in an outer transaction
        // breaks migrations containing non-transactional operations, and suppresses the
        // database-wide lock EF takes to keep concurrent hosts from migrating at once
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
