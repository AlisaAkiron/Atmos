namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A reciprocal link to another site, as shown to visitors.
/// </summary>
/// <param name="Url">Absolute URL the link points at.</param>
/// <param name="Title">Name of the linked site.</param>
/// <param name="Description">Short blurb describing the linked site.</param>
/// <param name="AvatarUrl">Public URL of the site's avatar, or null when none is set.</param>
public record FriendLinkDto(
    string Url,
    string Title,
    string Description,
    string? AvatarUrl);
