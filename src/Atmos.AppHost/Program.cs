using Atmos.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

builder.AddProject<Projects.Atmos_Api>("AtmosApi");

var app = builder.Build();

await app.RunAsync();
