namespace Atmos.Api.Endpoints.Dto;

public record FriendLinkDto(
    string Url,
    string Title,
    string Description,
    string? AvatarUrl);
