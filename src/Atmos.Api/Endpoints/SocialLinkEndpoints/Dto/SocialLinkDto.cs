namespace Atmos.Api.Endpoints.Dto;

public record SocialLinkDto(
    string Url,
    string Label,
    string Color,
    bool Invert,
    string IconUrl);
