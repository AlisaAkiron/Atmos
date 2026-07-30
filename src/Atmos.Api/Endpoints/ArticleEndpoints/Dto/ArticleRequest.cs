namespace Atmos.Api.Endpoints.Dto;

public record ArticleRequest(
    string Slug,
    string Title,
    string? Summary,
    string Content,
    string? CategorySlug,
    List<string> TagSlugs);
