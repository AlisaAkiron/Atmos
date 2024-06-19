using Atmos.Services.Default;
using Atmos.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddAtmosDefaultServices();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapAtmosDefaultEndpoints();

app.UseStaticFiles();
app.UseAntiforgery();

app
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
