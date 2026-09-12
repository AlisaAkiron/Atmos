namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of a category. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Slug">URL segment to use. Must be a valid slug and not already taken.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Longer blurb for the category page. Null for none.</param>
public record CategoryRequest(
    string Slug,
    string Name,
    string? Description);
