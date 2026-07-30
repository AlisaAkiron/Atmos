namespace Atmos.Services.Media.Abstract;

public interface IMediaStorage
{
    public Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    public Task DeleteAsync(string key, CancellationToken ct = default);

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
