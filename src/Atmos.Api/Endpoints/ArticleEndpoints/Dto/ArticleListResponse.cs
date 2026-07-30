namespace Atmos.Api.Endpoints.Dto;

public record ArticleListItemDto(
    string Slug,
    string Title,
    string? Summary,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset PublishedAt);

public record ArticleListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<ArticleListItemDto> Items);
