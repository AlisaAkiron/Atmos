using System.ComponentModel.DataAnnotations;
namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of a profile link. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Url">Absolute URL of the profile.</param>
/// <param name="Label">Name of the platform, used as the accessible label.</param>
/// <param name="Color">Brand colour for the icon, as a CSS colour string.</param>
/// <param name="Invert">Whether the icon needs inverting to stay legible on a dark background.</param>
/// <param name="IconMediaId">Media entry to use as the icon. Must already exist.</param>
/// <param name="DisplayOrder">Sort position; lower values come first.</param>
public record SocialLinkRequest(
    [property: Required, Url, StringLength(2048)] string Url,
    [property: Required, StringLength(128)] string Label,
    [property: Required, StringLength(64)] string Color,
    bool Invert,
    [property: Required] Guid IconMediaId,
    int DisplayOrder);
