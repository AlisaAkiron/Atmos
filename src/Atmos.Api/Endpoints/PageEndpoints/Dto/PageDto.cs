namespace Atmos.Api.Endpoints.Dto;

public record PageDto(
    string Slug,
    string Title,
    string Content,
    DateTimeOffset PublishedAt);
