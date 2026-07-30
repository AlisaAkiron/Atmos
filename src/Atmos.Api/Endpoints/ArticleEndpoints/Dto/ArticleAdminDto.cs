namespace Atmos.Api.Endpoints.Dto;

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

public record ArticleAdminListItemDto(
    Guid ArticleId,
    string Slug,
    string Title,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset? PublishedAt,
    DateTimeOffset UpdateAt);

public record ArticleAdminListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<ArticleAdminListItemDto> Items);
