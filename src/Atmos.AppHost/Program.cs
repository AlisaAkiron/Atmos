using Atmos.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

var postgres = builder
    .AddPostgres("postgresql-instance")
    .AddDatabase("postgresql-database");

var apiContent = builder
    .AddProject<Projects.Atmos_Api_Content>("api-content")
    .WithReference(postgres);

builder
    .AddProject<Projects.Atmos_Web>("web")
    .WithReference(apiContent);

var app = builder.Build();

await app.RunAsync();
