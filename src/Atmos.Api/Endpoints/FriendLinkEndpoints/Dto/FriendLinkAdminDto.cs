namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A friend link as the site owner manages it, carrying the IDs the public view omits.
/// </summary>
/// <param name="FriendLinkId">Identifier the admin routes address this link by.</param>
/// <param name="Url">Absolute URL the link points at.</param>
/// <param name="Title">Name of the linked site.</param>
/// <param name="Description">Short blurb describing the linked site.</param>
/// <param name="AvatarMediaId">Media entry used as the avatar, or null when none is set.</param>
/// <param name="AvatarUrl">Public URL resolved from the avatar media, or null when none is set.</param>
/// <param name="DisplayOrder">Sort position; lower values come first.</param>
public record FriendLinkAdminDto(
    Guid FriendLinkId,
    string Url,
    string Title,
    string Description,
    Guid? AvatarMediaId,
    string? AvatarUrl,
    int DisplayOrder);
