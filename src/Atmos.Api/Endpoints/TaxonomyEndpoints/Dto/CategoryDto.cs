namespace Atmos.Api.Endpoints.Dto;

public record CategoryDto(
    string Slug,
    string Name,
    string? Description,
    int ArticleCount);

public record CategoryAdminDto(
    Guid CategoryId,
    string Slug,
    string Name,
    string? Description,
    int ArticleCount);
