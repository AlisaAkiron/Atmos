namespace Atmos.Api.Endpoints.Dto;

public record FriendLinkAdminDto(
    Guid FriendLinkId,
    string Url,
    string Title,
    string Description,
    Guid? AvatarMediaId,
    string? AvatarUrl,
    int DisplayOrder);
