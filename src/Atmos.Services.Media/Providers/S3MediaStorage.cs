using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Atmos.Services.Media.Abstract;
using Atmos.Services.Media.Options;
using Microsoft.Extensions.Options;

namespace Atmos.Services.Media.Providers;

public class S3MediaStorage : IMediaStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucket;

    public S3MediaStorage(IAmazonS3 s3Client, IOptions<MediaOptions> options)
    {
        _s3Client = s3Client;
        _bucket = options.Value.S3.Bucket;
    }

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // R2 does not accept aws-chunked streaming uploads; sign the full payload instead
            UseChunkEncoding = false
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _s3Client.DeleteObjectAsync(_bucket, key, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucket, key, ct);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
