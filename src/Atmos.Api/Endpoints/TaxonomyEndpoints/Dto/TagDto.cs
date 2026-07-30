namespace Atmos.Api.Endpoints.Dto;

public record TagDto(
    string Slug,
    string Name,
    int ArticleCount);

public record TagAdminDto(
    Guid TagId,
    string Slug,
    string Name,
    int ArticleCount);
