namespace Atmos.Api.Endpoints.Dto;

public record MediaReferrerDto(
    string ReferrerType,
    Guid ReferrerId);

public record MediaAdminDto(
    Guid MediaId,
    string Key,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string Sha256,
    int RefCount,
    List<MediaReferrerDto> Referrers);
