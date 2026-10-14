using System.ComponentModel.DataAnnotations;
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
    [property: Required, StringLength(128)] string Slug,
    [property: Required, StringLength(256)] string Title,
    [property: StringLength(1024)] string? Summary,
    [property: Required(AllowEmptyStrings = true), StringLength(1_000_000)] string Content,
    [property: StringLength(128)] string? CategorySlug,
    [property: Required, MaxLength(64)] List<string> TagSlugs);
