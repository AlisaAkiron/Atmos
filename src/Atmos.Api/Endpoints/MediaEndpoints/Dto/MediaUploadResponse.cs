namespace Atmos.Api.Endpoints.Dto;

public record MediaUploadResponse(
    Guid MediaId,
    string Key,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string? DuplicateOfKey);
