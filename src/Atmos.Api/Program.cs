using Atmos.Services.Default;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();

var app = builder.Build();

app.MapAtmosDefaultEndpoints();

await app.RunAsync();
