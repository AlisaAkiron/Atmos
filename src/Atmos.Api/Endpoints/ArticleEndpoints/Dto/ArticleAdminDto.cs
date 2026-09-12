namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// An article as its author sees it, drafts included.
/// </summary>
/// <param name="ArticleId">Identifier the admin routes address this article by.</param>
/// <param name="Slug">URL segment the article is published under.</param>
/// <param name="Title">Article title.</param>
/// <param name="Summary">Short standfirst shown in listings, or null when none is written.</param>
/// <param name="Content">Full article body, in Markdown.</param>
/// <param name="CategorySlug">Slug of the category the article is filed under, or null when uncategorised.</param>
/// <param name="TagSlugs">Slugs of every tag attached to the article.</param>
/// <param name="PublishedAt">When the article went public, or null while it is still a draft.</param>
/// <param name="CreateAt">When the article was first created.</param>
/// <param name="UpdateAt">When the article was last edited.</param>
public record ArticleAdminDto(
    Guid ArticleId,
    string Slug,
    string Title,
    string? Summary,
    string Content,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt);

/// <summary>
/// One row of the admin article list. Carries no body, so listings stay cheap.
/// </summary>
/// <param name="ArticleId">Identifier the admin routes address this article by.</param>
/// <param name="Slug">URL segment the article is published under.</param>
/// <param name="Title">Article title.</param>
/// <param name="CategorySlug">Slug of the category the article is filed under, or null when uncategorised.</param>
/// <param name="TagSlugs">Slugs of every tag attached to the article.</param>
/// <param name="PublishedAt">When the article went public, or null while it is still a draft.</param>
/// <param name="UpdateAt">When the article was last edited.</param>
public record ArticleAdminListItemDto(
    Guid ArticleId,
    string Slug,
    string Title,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdateAt);

/// <summary>
/// A page of the admin article list.
/// </summary>
/// <param name="TotalCount">Articles matching the query across every page.</param>
/// <param name="Page">Page number this response covers, counting from 1.</param>
/// <param name="PageSize">Articles per page, after clamping to the server maximum.</param>
/// <param name="Items">The articles on this page, newest first.</param>
public record ArticleAdminListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<ArticleAdminListItemDto> Items);
