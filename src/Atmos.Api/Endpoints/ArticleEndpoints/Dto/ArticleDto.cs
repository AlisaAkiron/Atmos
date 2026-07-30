namespace Atmos.Api.Endpoints.Dto;

public record ArticleDto(
    string Slug,
    string Title,
    string? Summary,
    string Content,
    string? CategorySlug,
    List<string> TagSlugs,
    DateTimeOffset PublishedAt);
