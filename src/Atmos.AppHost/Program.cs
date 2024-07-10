using Atmos.AppHost;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

#region Parameters

var postgresqlPassword = builder
    .AddParameter("postgresql-password", true);

var versions = builder.GetComponentVersion();

#endregion

#region External Services

var postgresqlInstance = builder
    .AddPostgres("postgresql-instance", password: postgresqlPassword)
    .WithOtlpExporter()
    .WithImageTag(versions.PostgreSqlTag)
    .WithDataVolume("atmos-db-volume")
    .WithPgAdmin(resourceBuilder =>
    {
        resourceBuilder.WithImageTag(versions.PgAdminTag);
    });

var postgresqlDatabase = postgresqlInstance
    .AddDatabase("postgresql-database", databaseName: "dev-atmos");

var redis = builder
    .AddRedis("redis")
    .WithImageTag(versions.RedisTag)
    .WithOtlpExporter();

#endregion

var apiContent = builder
    .AddProject<Projects.Atmos_Api_Content>("api-content")
    .WithReference(postgresqlDatabase)
    .WithReference(redis);

builder
    .AddProject<Projects.Atmos_Web>("web")
    .WithReference(apiContent);

builder.AddProject<Atmos_Worker_Migrator>("worker-migrator")
    .WithReference(postgresqlDatabase);

var app = builder.Build();

await app.RunAsync();
