using Atmos.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

#region Parameters

var postgresqlTag = builder.AddParameter("postgresql-tag", "17.0").GetString();
var pgAdminTag = builder.AddParameter("pgadmin-tag", "latest").GetString();
var redisTag = builder.AddParameter("redis-tag", "alpine").GetString();
var redisCommanderTag = builder.AddParameter("redis-commander-tag", "latest").GetString();

var withPgAdmin = builder.AddParameter("with-pgadmin", "false").GetBool();
var withRedisCommander = builder.AddParameter("with-redis-commander", "false").GetBool();

var postgresqlPassword = builder
    .AddParameter("postgresql-password", "1nyWacUqpb3NMd8BUECiZkP51VHNYaxL", false, true);

#endregion

#region External Services

// PostgreSQL
var postgresqlInstance = builder
    .AddPostgres("postgresql-instance", password: postgresqlPassword)
    .WithOtlpExporter()
    .WithImageTag(postgresqlTag)
    .WithDataVolume("atmos-db-volume")
    .WithLifetime(ContainerLifetime.Persistent);

if (withPgAdmin)
{
    postgresqlInstance.WithPgAdmin(resourceBuilder =>
    {
        resourceBuilder
            .WithImageTag(pgAdminTag)
            .WithLifetime(ContainerLifetime.Persistent);
    });
}

var postgresqlDatabase = postgresqlInstance
    .AddDatabase("postgresql-database", databaseName: "dev-atmos");

// Redis
var redis = builder
    .AddRedis("redis")
    .WithImageTag(redisTag)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithClearCommand()
    .WithOtlpExporter();

if (withRedisCommander)
{
    redis.WithRedisCommander(resourceBuilder =>
    {
        resourceBuilder
            .WithImageTag(redisCommanderTag)
            .WithLifetime(ContainerLifetime.Persistent);
    });
}

#endregion

var migrator = builder.AddProject<Atmos_Worker_Migrator>("worker-migrator")
    .WithReference(postgresqlDatabase)
    .WaitFor(postgresqlDatabase);

var apiContent = builder
    .AddProject<Atmos_Api_Content>("api-content")
    .WithReference(postgresqlDatabase)
    .WithReference(redis)
    .WaitForCompletion(migrator);

builder
    .AddProject<Atmos_Web>("web")
    .WithReference(apiContent)
    .WaitForCompletion(migrator);

var app = builder.Build();

await app.RunAsync();
