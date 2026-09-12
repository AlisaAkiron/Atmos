using System.Text.Json;
using Atmos.Services.Api;
using Atmos.Services.Api.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Atmos.Tests.Unit.OpenApi;

public class AuthenticationTransformerTests
{
    private static OpenApiOperation TransformOperation(params object[] endpointMetadata)
    {
        return TransformOperation(BuildDocument(), endpointMetadata);
    }

    private static OpenApiDocument BuildDocument()
    {
        var document = new OpenApiDocument();

        var context = new OpenApiDocumentTransformerContext
        {
            DocumentName = "api",
            DescriptionGroups = [],
            ApplicationServices = new ServiceCollection().BuildServiceProvider()
        };

        new AuthenticationTransformer()
            .TransformAsync(document, context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return document;
    }

    private static OpenApiOperation TransformOperation(OpenApiDocument document, params object[] endpointMetadata)
    {
        var operation = new OpenApiOperation();
        var description = new ApiDescription
        {
            ActionDescriptor = new ActionDescriptor
            {
                EndpointMetadata = [.. endpointMetadata]
            }
        };

        var context = new OpenApiOperationTransformerContext
        {
            DocumentName = "api",
            Description = description,
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
            Document = document
        };

        new AuthenticationTransformer()
            .TransformAsync(operation, context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return operation;
    }

    [Test]
    public async Task Declares_The_Session_Cookie_As_A_Security_Scheme()
    {
        var document = BuildDocument();

        var scheme = document.Components?.SecuritySchemes?[AtmosAuthenticationDefaults.Scheme];

        await Assert.That(scheme).IsNotNull();
        await Assert.That(scheme!.Type).IsEqualTo(SecuritySchemeType.ApiKey);
        await Assert.That(scheme.In).IsEqualTo(ParameterLocation.Cookie);
        await Assert.That(scheme.Name).IsEqualTo(AtmosAuthenticationDefaults.Scheme);
    }

    [Test]
    public async Task Leaves_An_Anonymous_Operation_Alone()
    {
        var operation = TransformOperation();

        await Assert.That(operation.Security).IsNull();
        await Assert.That(operation.Responses?.ContainsKey("401") ?? false).IsFalse();
        await Assert.That(operation.Responses?.ContainsKey("403") ?? false).IsFalse();
    }

    [Test]
    public async Task An_Operation_Needing_Only_A_Session_Documents_401_But_Not_403()
    {
        var operation = TransformOperation(new AuthorizeAttribute());

        await Assert.That(operation.Security).IsNotNull();
        await Assert.That(operation.Security!.Count).IsEqualTo(1);
        await Assert.That(operation.Responses!.ContainsKey("401")).IsTrue();
        await Assert.That(operation.Responses.ContainsKey("403")).IsFalse();
    }

    [Test]
    public async Task An_Operation_Behind_A_Policy_Also_Documents_403()
    {
        var operation = TransformOperation(
            new AuthorizeAttribute(AtmosAuthenticationDefaults.SiteOwnerPolicy));

        await Assert.That(operation.Responses!.ContainsKey("401")).IsTrue();
        await Assert.That(operation.Responses.ContainsKey("403")).IsTrue();
    }

    [Test]
    public async Task The_Requirement_Serialises_As_A_Reference_To_The_Declared_Scheme()
    {
        // A requirement built without the host document resolves to nothing and writes an
        // empty object, which no reader can tie back to the security scheme
        var document = BuildDocument();
        var operation = TransformOperation(document, new AuthorizeAttribute());

        document.Paths = new OpenApiPaths
        {
            ["/probe"] = new OpenApiPathItem
            {
                Operations = new Dictionary<HttpMethod, OpenApiOperation> { [HttpMethod.Get] = operation }
            }
        };

        using var stream = new MemoryStream();
        await document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_1);

        var written = JsonDocument.Parse(stream.ToArray());
        var security = written.RootElement
            .GetProperty("paths").GetProperty("/probe").GetProperty("get").GetProperty("security");

        await Assert.That(security[0].TryGetProperty(AtmosAuthenticationDefaults.Scheme, out _)).IsTrue();
    }
}
