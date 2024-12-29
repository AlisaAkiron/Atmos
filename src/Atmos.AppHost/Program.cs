using Atmos.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

#region Parameters

var postgresqlTag = builder.AddParameter("postgresql-tag", "17.0").GetString();
var redisTag = builder.AddParameter("redis-tag", "alpine").GetString();

var postgresqlPassword = builder
    .AddParameter("postgresql-password", "1nyWacUqpb3NMd8BUECiZkP51VHNYaxL", false, true);

var enablePgadmin = builder.AddParameter("enable-pgadmin", "false").GetBool();
var enableRedisCommander = builder.AddParameter("enable-redis-commander", "false").GetBool();

#endregion

#region External Services

var postgresql = builder
    .AddResourceWithConnectionString(b =>
    {
        var pg = b
            .AddPostgres("postgresql-instance", password: postgresqlPassword)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithOtlpExporter()
            .WithImageTag(postgresqlTag)
            .WithDataVolume("atmos-db-volume");
        if (enablePgadmin)
        {
            pg.WithPgAdmin(pgadmin => pgadmin
                .WithImageTag("latest")
                .WithLifetime(ContainerLifetime.Persistent));
        }

        pg.AddDatabase("postgresql-database", "dev-atmos");
        return pg;
    }, "PostgreSQL");
var redis = builder
    .AddResourceWithConnectionString(b =>
    {
        var r = b
            .AddRedis("redis")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithClearCommand()
            .WithOtlpExporter()
            .WithImageTag(redisTag)
            .WithDataVolume("atmos-redis");
        if (enableRedisCommander)
        {
            r.WithRedisCommander(rc => rc
                .WithImageTag("latest")
                .WithLifetime(ContainerLifetime.Persistent));
        }

        return r;
    }, "Redis");

#endregion

var migrator = builder.AddProject<Atmos_Worker_Migrator>("worker-migrator")
    .WithReference(postgresql, "PostgreSQL")
    .WaitFor(postgresql);

var api = builder
    .AddProject<Atmos_Api>("api")
    .WithReference(postgresql, "PostgreSQL")
    .WithReference(redis, "Redis")
    .WaitForCompletion(migrator);

builder
    .AddProject<Atmos_Web>("web")
    .WithReference(api)
    .WaitForCompletion(migrator);

var app = builder.Build();

await app.RunAsync();
