namespace Atmos.Api.Endpoints.Dto;

public record PageRequest(
    string Slug,
    string Title,
    string Content);
