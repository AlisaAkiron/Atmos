namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of a standalone page. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Slug">URL segment to publish under. Must be a valid slug and not already taken.</param>
/// <param name="Title">Page title.</param>
/// <param name="Content">Full page body, in Markdown.</param>
public record PageRequest(
    string Slug,
    string Title,
    string Content);
