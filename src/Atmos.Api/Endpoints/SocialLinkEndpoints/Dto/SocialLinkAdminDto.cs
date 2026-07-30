namespace Atmos.Api.Endpoints.Dto;

public record SocialLinkAdminDto(
    Guid SocialLinkId,
    string Url,
    string Label,
    string Color,
    bool Invert,
    Guid IconMediaId,
    string IconUrl,
    int DisplayOrder);
