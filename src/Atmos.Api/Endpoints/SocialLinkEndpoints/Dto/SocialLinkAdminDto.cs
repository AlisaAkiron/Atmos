namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A profile link as the site owner manages it, carrying the IDs the public view omits.
/// </summary>
/// <param name="SocialLinkId">Identifier the admin routes address this link by.</param>
/// <param name="Url">Absolute URL of the profile.</param>
/// <param name="Label">Name of the platform, used as the accessible label.</param>
/// <param name="Color">Brand colour for the icon, as a CSS colour string.</param>
/// <param name="Invert">Whether the icon needs inverting to stay legible on a dark background.</param>
/// <param name="IconMediaId">Media entry used as the icon.</param>
/// <param name="IconUrl">Public URL resolved from the icon media.</param>
/// <param name="DisplayOrder">Sort position; lower values come first.</param>
public record SocialLinkAdminDto(
    Guid SocialLinkId,
    string Url,
    string Label,
    string Color,
    bool Invert,
    Guid IconMediaId,
    string IconUrl,
    int DisplayOrder);
