using Atmos.Services.Api.OpenApi;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;

namespace Atmos.Tests.Unit.OpenApi;

public class OpenApiConfiguratorTests
{
    [Test]
    public async Task Includes_Endpoints_Grouped_By_Api_Version()
    {
        var options = new OpenApiOptions();

        OpenApiConfigurator.ConfigureDocument(options);

        // Endpoints mapped through NewVersionedApi() are grouped by API version,
        // never by the service name the single Atmos document is called
        var description = new ApiDescription
        {
            GroupName = "1",
            RelativePath = "api/articles"
        };

        await Assert.That(options.ShouldInclude(description)).IsTrue();
    }
}
