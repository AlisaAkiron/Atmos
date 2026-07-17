using Atmos.Api.Endpoints;
using Atmos.Services.Api;
using Atmos.Services.Default;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();
builder.AddAtmosApiServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Exposes token/claim values in identity-model logs; never enable outside development
    IdentityModelEventSource.ShowPII = true;
}

app.MapAtmosDefaultEndpoints();

app.MapAtmosApiEndpoints(api =>
{
    api.MapEndpoints<AuthenticationEndpoints>();

    if (app.Environment.IsProduction() is false)
    {
        api.MapEndpoints<DebugEndpoints>();
    }
});

await app.RunAsync();
