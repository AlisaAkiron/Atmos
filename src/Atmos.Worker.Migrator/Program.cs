using Atmos.Services.Api.Components;
using Atmos.Services.Default;
using Atmos.Worker.Migrator;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

if (EF.IsDesignTime is false)
{
    builder.AddAtmosDefaultServices();
    builder.Services.AddHostedService<Worker>();
}

builder.ConfigureNpgsql();

var app = builder.Build();

await app.RunAsync();
