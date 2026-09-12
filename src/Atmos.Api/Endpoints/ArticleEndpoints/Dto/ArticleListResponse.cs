namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// One row of the public article list. Carries no body, so listings stay cheap.
/// </summary>
/// <param name="Slug">URL segment the article is published under.</param>
/// <param name="Title">Article title.</param>
/// <param name="Summary">Short standfirst shown in listings, or null when none is written.</param>
/// <param name="CategorySlug">Slug of the category the article is filed under, or null when uncategorised.</param>
/// <param name="TagSlugs">Slugs of every tag attached to the article.</param>
/// <param name="PublishedAt">When the article went public.</param>
public record ArticleListItemDto(
    string Slug,
    string Title,
    string? Summary,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset PublishedAt);

/// <summary>
/// A page of the public article list.
/// </summary>
/// <param name="TotalCount">Published articles matching the query across every page.</param>
/// <param name="Page">Page number this response covers, counting from 1.</param>
/// <param name="PageSize">Articles per page, after clamping to the server maximum.</param>
/// <param name="Items">The articles on this page, newest first.</param>
public record ArticleListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<ArticleListItemDto> Items);
