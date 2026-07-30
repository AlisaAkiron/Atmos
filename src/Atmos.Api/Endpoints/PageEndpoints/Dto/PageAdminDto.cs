namespace Atmos.Api.Endpoints.Dto;

public record PageAdminDto(
    Guid PageId,
    string Slug,
    string Title,
    string Content,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt);
