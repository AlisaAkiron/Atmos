using Atmos.Api.Endpoints;
using Atmos.Database;
using Atmos.Services.Api;
using Atmos.Services.Api.OpenApi;
using Atmos.Services.Default;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

// This project is the startup project for `dotnet ef`, which executes the file up to
// Build() to discover AtmosDbContext. The tooling needs the context and its provider
// and nothing else, so skip the rest: building the full graph would make scaffolding a
// migration depend on valid authentication, media and telemetry configuration.
if (EF.IsDesignTime)
{
    builder.ConfigureNpgsql();
}
else
{
    builder.AddAtmosDefaultServices();
    builder.AddAtmosApiServices();

    // Registered here rather than inside AddAtmosApiServices: the XML comment support in
    // Microsoft.AspNetCore.OpenApi is a source generator that intercepts this call site and
    // reads comments from its own compilation, which is where the endpoints and DTOs live.
    builder.Services.AddOpenApi(
        OpenApiConfigurator.DocumentName(builder.Configuration),
        OpenApiConfigurator.ConfigureDocument);

    // Registered here for the same reason: the validation source generator discovers
    // request types from its own compilation, which is where the DTOs live.
    builder.Services.AddValidation();
}

var app = builder.Build();

// Nothing below here runs at design time: the EF tooling stops the host at Build()
await app.MigrateAtmosDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    // Exposes token/claim values in identity-model logs; never enable outside development
    IdentityModelEventSource.ShowPII = true;
}

app.MapAtmosDefaultEndpoints();

app.MapAtmosApiEndpoints(api =>
{
    api.MapEndpoints<AuthenticationEndpoints>();
    api.MapEndpoints<SocialLinkEndpoints>();
    api.MapEndpoints<FriendLinkEndpoints>();
    api.MapEndpoints<TaxonomyEndpoints>();
    api.MapEndpoints<ArticleEndpoints>();
    api.MapEndpoints<PageEndpoints>();
    api.MapEndpoints<MediaEndpoints>();

    if (app.Environment.IsProduction() is false)
    {
        api.MapEndpoints<DebugEndpoints>();
    }
});

await app.RunAsync();
