using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Atmos.Services.Media.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media.Services;

/// <summary>
/// Dev-only startup step for RustFS: creates the bucket and opens anonymous
/// read access so PublicBaseUrl works like the production R2 custom domain.
/// </summary>
public class BucketInitializer : IHostedService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;

    public BucketInitializer(IAmazonS3 s3Client, IOptions<MediaOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value.S3;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucket = _options.Bucket;

        try
        {
            await _s3Client.PutBucketAsync(bucket, cancellationToken);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.Conflict)
        {
            // BucketAlreadyOwnedByYou: fine, it exists from a previous run
        }

        var policy = $$"""
        {
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Effect": "Allow",
                    "Principal": { "AWS": ["*"] },
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{bucket}}/*"]
                }
            ]
        }
        """;

        await _s3Client.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = bucket,
            Policy = policy
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
