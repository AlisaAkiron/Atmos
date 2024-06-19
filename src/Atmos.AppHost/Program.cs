using Atmos.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

var postgres = builder
    .AddPostgres("postgresql-instance")
    .AddDatabase("postgresql-database");

var redis = builder
    .AddRedis("redis");

builder
    .AddProject<Projects.Atmos_Api_Content>("atmos-api")
    .WithReference(postgres)
    .WithReference(redis);

var app = builder.Build();

await app.RunAsync();
