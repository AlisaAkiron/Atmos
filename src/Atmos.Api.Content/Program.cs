using Atmos.Services.Api;
using Atmos.Services.Default;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();
builder.AddAtmosApiServices();

var app = builder.Build();

app.MapAtmosDefaultEndpoints();

app.MapGet("/", () => "Hello World! (Content API)");

await app.RunAsync();
