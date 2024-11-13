using System.Diagnostics;
using Atmos.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Atmos.Worker.Migrator;

public class Worker : BackgroundService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public Worker(
        IHostEnvironment hostEnvironment,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _hostEnvironment = hostEnvironment;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AtmosDbContext>();

            await EnsureDatabaseAsync(dbContext, stoppingToken);
            await RunMigrationAsync(dbContext, stoppingToken);

            if (_hostEnvironment.IsDevelopment())
            {
                await SeedDevelopmentDataAsync(dbContext, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();
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

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteInTransactionAsync(
            dbContext,
            async (context, ct) =>
            {
                await context.Database.MigrateAsync(ct);
            },
            async (context, ct) =>
            {
                var migrations = await context.Database.GetPendingMigrationsAsync(ct);
                return migrations.Any();
            },
            cancellationToken);
    }

    private static async Task SeedDevelopmentDataAsync(AtmosDbContext dbContext, CancellationToken cancellationToken)
    {
        _ = dbContext;
        _ = cancellationToken;
        await Task.CompletedTask;
    }
}
