using Atmos.Api.Endpoints;
using Atmos.Services.Api;
using Atmos.Services.Api.OpenApi;
using Atmos.Services.Default;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();
builder.AddAtmosApiServices();

// Registered here rather than inside AddAtmosApiServices: the XML comment support in
// Microsoft.AspNetCore.OpenApi is a source generator that intercepts this call site and
// reads comments from its own compilation, which is where the endpoints and DTOs live.
builder.Services.AddOpenApi(
    OpenApiConfigurator.DocumentName(builder.Configuration),
    OpenApiConfigurator.ConfigureDocument);

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
