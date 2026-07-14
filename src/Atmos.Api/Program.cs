using Atmos.Services.Api;
using Atmos.Services.Default;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();
builder.AddAtmosApiServices();

var app = builder.Build();

IdentityModelEventSource.ShowPII = true;

app.MapAtmosDefaultEndpoints();

app.MapAtmosApiEndpoints();

await app.RunAsync();
