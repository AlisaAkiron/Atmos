namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The full desired state of an article. Update replaces every field, so omitting one clears it.
/// </summary>
/// <param name="Slug">URL segment to publish under. Must be a valid slug and not already taken.</param>
/// <param name="Title">Article title.</param>
/// <param name="Summary">Short standfirst for listings. Null for none.</param>
/// <param name="Content">Full article body, in Markdown.</param>
/// <param name="CategorySlug">Slug of an existing category to file under, or null to leave it uncategorised.</param>
/// <param name="TagSlugs">Slugs of existing tags to attach. Every slug must already exist.</param>
public record ArticleRequest(
    string Slug,
    string Title,
    string? Summary,
    string Content,
    string? CategorySlug,
    List<string> TagSlugs);
