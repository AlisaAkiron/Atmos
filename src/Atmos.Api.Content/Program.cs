using Atmos.Services.Api;
using Atmos.Services.Default;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();
builder.AddAtmosApiServices();

var app = builder.Build();

app.MapAtmosDefaultEndpoints();

app.MapAtmosOpenApiEndpoints();
app.MapAtmosApiEndpoints();

await app.RunAsync();
