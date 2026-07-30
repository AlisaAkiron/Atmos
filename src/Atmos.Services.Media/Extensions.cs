using Amazon.S3;
using Atmos.Common.Extensions;
using Atmos.Services.Media.Abstract;
using Atmos.Services.Media.Enums;
using Atmos.Services.Media.Options;
using Atmos.Services.Media.Providers;
using Atmos.Services.Media.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media;

public static class Extensions
{
    public static IHostApplicationBuilder AddAtmosMediaServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection("Media"));

        // Resolve the local root exactly once, here, so the provider, the directory
        // initializer, and the static-file middleware all agree on one absolute path.
        // PostConfigure (rather than a second Configure) makes "runs after binding"
        // structural instead of dependent on registration order.
        var contentRootPath = builder.Environment.ContentRootPath;
        builder.Services.PostConfigure<MediaOptions>(options =>
        {
            if (string.IsNullOrEmpty(options.Local.RootPath))
            {
                options.Local.RootPath = Path.Combine(contentRootPath, "media-storage");
            }

            // Normalize a relative RootPath (including the fallback above, which is
            // already absolute and so unaffected) against the content root, not the
            // process working directory. Otherwise a relative value like
            // "data/media" reaches PhysicalFileProvider, which throws "The path
            // must be absolute" at startup - but only after Directory.CreateDirectory
            // has already created a stray directory at the working directory.
            options.Local.RootPath = Path.GetFullPath(options.Local.RootPath, contentRootPath);
        });

        builder.Services.AddSingleton<MediaUrlBuilder>();
        builder.Services.AddScoped<MediaReferenceService>();

        var mediaOptions = builder.Configuration.GetOptions<MediaOptions>("Media");

        if (mediaOptions.Provider is MediaProviderType.Local)
        {
            builder.Services.AddSingleton<IMediaStorage, LocalMediaStorage>();
            builder.Services.AddHostedService<MediaDirectoryInitializer>();

            return builder;
        }

        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MediaOptions>>().Value.S3;

            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = true,
                // R2 does not implement the newer default integrity checksums
                RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED
            };

            return new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, config);
        });

        builder.Services.AddSingleton<IMediaStorage, S3MediaStorage>();

        if (mediaOptions.S3.EnsureBucketOnStartup)
        {
            builder.Services.AddHostedService<BucketInitializer>();
        }

        return builder;
    }

    /// <summary>
    /// Serves the local media directory. No-op for the S3 provider, where media is
    /// served by the bucket's public endpoint instead.
    /// </summary>
    public static WebApplication UseAtmosMediaFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<MediaOptions>>().Value;
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Atmos.Services.Media");

        if (options.Provider is not MediaProviderType.Local)
        {
            // Never log AccessKeyId / SecretAccessKey.
            logger.LogInformation(
                "Media provider: {Provider}. Bucket: {Bucket}, ServiceUrl: {ServiceUrl}",
                options.Provider, options.S3.Bucket, options.S3.ServiceUrl);

            return app;
        }

        ValidateLocalServingOptions(options);

        logger.LogInformation(
            "Media provider: {Provider}. RootPath: {RootPath}, RequestPath: {RequestPath}",
            options.Provider, options.Local.RootPath, options.Local.RequestPath);

        // PhysicalFileProvider throws if the directory is absent, and this runs
        // before MediaDirectoryInitializer (hosted services start after the
        // pipeline is built), so create it here too. Both calls are idempotent.
        Directory.CreateDirectory(options.Local.RootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(options.Local.RootPath),
            RequestPath = options.Local.RequestPath.TrimEnd('/'),
            // Uploads may carry extensions the MIME map does not know; serving them
            // as octet-stream beats a 404
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
            OnPrepareResponse = context =>
            {
                // Delete is only blocked while a media's reference count is above
                // zero: upload -> preview -> delete (count back to 0) -> re-upload
                // at the same key is reachable, so keys are NOT immutable. Marking
                // the response "immutable" would make already-cached clients serve
                // stale bytes for a year with no way to revalidate, even on a hard
                // reload.
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000";

                // On the Local provider, media shares the API's origin (and its
                // SameSite=Lax auth cookie) instead of a separate R2 host, and
                // ServeUnknownFileTypes=true is set above. Uploads are
                // SiteOwner-only so this is self-XSS rather than a remote hole,
                // but the origin isolation R2 gave us for free is worth keeping.
                context.Context.Response.Headers.XContentTypeOptions = "nosniff";
            }
        });

        return app;
    }

    /// <summary>
    /// Validates <see cref="LocalStorageOptions.RequestPath" /> once at startup,
    /// before the static-file middleware is constructed.
    /// </summary>
    private static void ValidateLocalServingOptions(MediaOptions options)
    {
        var requestPath = options.Local.RequestPath.TrimEnd('/');

        if (string.IsNullOrEmpty(requestPath))
        {
            // "/".TrimEnd('/') is "", and an empty match path makes
            // StartsWithSegments return true for EVERY request - putting a
            // filesystem probe ahead of the whole pipeline, and letting an
            // uploaded key like "api/social-links" shadow a real route.
            throw new InvalidOperationException(
                $"Media:Local:RequestPath ('{options.Local.RequestPath}') must not be empty or '/'.");
        }

        // Skip the cross-check when PublicBaseUrl is empty or not a well-formed
        // absolute URI, so unit tests and non-Local setups are unaffected.
        if (string.IsNullOrEmpty(options.PublicBaseUrl)
            || Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var publicBaseUri) is false)
        {
            return;
        }

        var publicUrlPath = publicBaseUri.AbsolutePath.TrimEnd('/');

        if (string.Equals(publicUrlPath, requestPath, StringComparison.OrdinalIgnoreCase) is false)
        {
            // LocalStorageOptions.RequestPath's own doc comment already says these
            // must match, but nothing enforced it - a mismatch means uploads
            // succeed while every stored URL 404s.
            throw new InvalidOperationException(
                $"Media:Local:RequestPath ('{options.Local.RequestPath}') must match the path segment of " +
                $"Media:PublicBaseUrl ('{options.PublicBaseUrl}'), but got '{publicUrlPath}'.");
        }
    }
}
