using System.ComponentModel.DataAnnotations;
namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of a tag. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Slug">URL segment to use. Must be a valid slug and not already taken.</param>
/// <param name="Name">Display name.</param>
public record TagRequest(
    [property: Required, StringLength(128)] string Slug,
    [property: Required, StringLength(128)] string Name);
