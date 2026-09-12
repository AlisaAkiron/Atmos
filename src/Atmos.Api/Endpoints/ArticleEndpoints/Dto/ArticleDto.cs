namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A published article, as shown to visitors.
/// </summary>
/// <param name="Slug">URL segment the article is published under.</param>
/// <param name="Title">Article title.</param>
/// <param name="Summary">Short standfirst shown in listings, or null when none is written.</param>
/// <param name="Content">Full article body, in Markdown.</param>
/// <param name="CategorySlug">Slug of the category the article is filed under, or null when uncategorised.</param>
/// <param name="TagSlugs">Slugs of every tag attached to the article.</param>
/// <param name="PublishedAt">When the article went public.</param>
public record ArticleDto(
    string Slug,
    string Title,
    string? Summary,
    string Content,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset PublishedAt);
