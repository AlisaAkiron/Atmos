using Atmos.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

#region Parameters

var postgresPassword = builder.AddParameter("postgres-password", "atmos", secret: true);
var redisPassword = builder.AddParameter("redis-password", "atmos", secret: true);

#endregion

#region External Services

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithImageTag("18.3")
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithVolume("atmos-psql-data", "/var/lib/postgresql")
    .WithHostPort(15432)
    .AddDatabase("psql-db", "atmos");

var redis = builder
    .AddRedis("redis", password: redisPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithImageTag("8.4.0-alpine")
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithDataVolume("atmos-redis-data")
    .WithHostPort(16379)
    .WithPersistence(TimeSpan.FromMinutes(5), 100);

var mailpit = builder
    .AddMailPit("mailpit")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithImageTag("v1.28.0")
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithDataVolume("atmos-mailpit-data");

#endregion

var migrator = builder.AddProject<Atmos_Worker_Migrator>("worker-migrator")
    .WithReference(postgres, "PostgreSQL")
    .WaitFor(postgres);

var api = builder
    .AddProject<Atmos_Api>("api")
    .WithReference(postgres, "PostgreSQL")
    .WithReference(redis, "Redis")
    .WithReference(mailpit, "Smtp")
    .WaitForCompletion(migrator);

builder
    .AddProject<Atmos_Web>("web")
    .WithReference(api)
    .WaitForCompletion(migrator);

var app = builder.Build();

await app.RunAsync();
