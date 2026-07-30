namespace Atmos.Api.Endpoints.Dto;

public record SocialLinkRequest(
    string Url,
    string Label,
    string Color,
    bool Invert,
    Guid IconMediaId,
    int DisplayOrder);
