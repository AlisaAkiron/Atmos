namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A link to one of the site owner's profiles, as shown to visitors.
/// </summary>
/// <param name="Url">Absolute URL of the profile.</param>
/// <param name="Label">Name of the platform, used as the accessible label.</param>
/// <param name="Color">Brand colour for the icon, as a CSS colour string.</param>
/// <param name="Invert">Whether the icon needs inverting to stay legible on a dark background.</param>
/// <param name="IconUrl">Public URL resolved from the icon media.</param>
public record SocialLinkDto(
    string Url,
    string Label,
    string Color,
    bool Invert,
    string IconUrl);
