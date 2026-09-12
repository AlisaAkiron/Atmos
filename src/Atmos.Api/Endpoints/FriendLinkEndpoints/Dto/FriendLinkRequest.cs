namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of a friend link. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Url">Absolute URL the link points at.</param>
/// <param name="Title">Name of the linked site.</param>
/// <param name="Description">Short blurb describing the linked site.</param>
/// <param name="AvatarMediaId">Media entry to use as the avatar. Must already exist; null for no avatar.</param>
/// <param name="DisplayOrder">Sort position; lower values come first.</param>
public record FriendLinkRequest(
    string Url,
    string Title,
    string Description,
    Guid? AvatarMediaId,
    int DisplayOrder);
