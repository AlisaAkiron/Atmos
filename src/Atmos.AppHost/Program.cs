using Atmos.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAtmosAppHost();

#region Parameters

var postgresPassword = builder.AddParameter("postgres-password", "atmos", secret: true);

// "Local" (default) keeps the dev loop container-free; "S3" starts RustFS and
// points the API at it, which is the only local way to exercise the R2 code path.
// Override: dotnet run --project src/Atmos.AppHost -- --media-provider S3
var mediaProvider = builder.Configuration["media-provider"] ?? "Local";
var useS3Media = string.Equals(mediaProvider, "S3", StringComparison.OrdinalIgnoreCase);

#endregion

#region External Services

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithImageTag("18.3")
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithVolume("atmos-psql-data", "/var/lib/postgresql")
    .WithHostPort(15432)
    .AddDatabase("psql-db", "atmos");

var mailpit = builder
    .AddMailPit("mailpit")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithOtlpExporter()
    .WithImageTag("v1.28.0")
    .WithImagePullPolicy(ImagePullPolicy.Missing)
    .WithDataVolume("atmos-mailpit-data");

#endregion

var migrator = builder.AddProject<Atmos_Worker_Migrator>("worker-migrator")
    .WithReference(postgres, "PostgreSQL")
    .WaitFor(postgres);

var api = builder
    .AddProject<Atmos_Api>("api")
    .WithReference(postgres, "PostgreSQL")
    .WithReference(mailpit, "Smtp")
    .WithEnvironment("Media__Provider", useS3Media ? "S3" : "Local")
    .WaitForCompletion(migrator);

if (useS3Media)
{
    var rustfsPassword = builder.AddParameter("rustfs-password", "atmos-dev-secret", secret: true);

    var rustfs = builder
        .AddContainer("rustfs", "rustfs/rustfs")
        .WithImageTag("1.0.0-beta.12")
        .WithImagePullPolicy(ImagePullPolicy.Missing)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithEnvironment("RUSTFS_VOLUMES", "/data")
        .WithEnvironment("RUSTFS_ADDRESS", "0.0.0.0:9000")
        .WithEnvironment("RUSTFS_CONSOLE_ADDRESS", "0.0.0.0:9001")
        .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
        .WithEnvironment("RUSTFS_ACCESS_KEY", "atmos")
        .WithEnvironment("RUSTFS_SECRET_KEY", rustfsPassword)
        .WithVolume("atmos-rustfs-data", "/data")
        .WithHttpEndpoint(port: 19000, targetPort: 9000, name: "s3")
        .WithHttpEndpoint(port: 19001, targetPort: 9001, name: "console")
        // BucketInitializer runs at API startup, so WaitFor must gate on readiness,
        // not just on the container being created
        .WithHttpHealthCheck(path: "/health", endpointName: "s3");

    var rustfsS3Endpoint = rustfs.GetEndpoint("s3");

    api
        .WithEnvironment("Media__S3__ServiceUrl", rustfsS3Endpoint)
        .WithEnvironment("Media__S3__AccessKeyId", "atmos")
        .WithEnvironment("Media__S3__SecretAccessKey", rustfsPassword)
        .WithEnvironment("Media__S3__Bucket", "atmos-resources")
        .WithEnvironment("Media__S3__EnsureBucketOnStartup", "true")
        .WithEnvironment("Media__PublicBaseUrl", ReferenceExpression.Create($"{rustfsS3Endpoint}/atmos-resources"))
        .WaitFor(rustfs);
}
else
{
    // Repo root, so uploads survive dotnet clean and are easy to inspect
    var localMediaPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".local-media"));

    // Self-referencing api.GetEndpoint("https") to build the public base URL. If this
    // comes out empty at runtime (or fails to resolve at model-build time), fall back to
    // the hardcoded dev URL below, which matches src/Atmos.Api/Properties/launchSettings.json:
    //     .WithEnvironment("Media__PublicBaseUrl", "https://localhost:7136/media");
    api
        .WithEnvironment("Media__Local__RootPath", localMediaPath)
        .WithEnvironment("Media__PublicBaseUrl", ReferenceExpression.Create($"{api.GetEndpoint("https")}/media"));
}

builder
    .AddProject<Atmos_Web>("web")
    .WithReference(api)
    .WaitForCompletion(migrator);

var app = builder.Build();

await app.RunAsync();
