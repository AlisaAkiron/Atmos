namespace Atmos.Api.Endpoints.Dto;

public record FriendLinkRequest(
    string Url,
    string Title,
    string Description,
    Guid? AvatarMediaId,
    int DisplayOrder);
