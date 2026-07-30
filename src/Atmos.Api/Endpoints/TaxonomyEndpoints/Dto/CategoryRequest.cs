namespace Atmos.Api.Endpoints.Dto;

public record CategoryRequest(
    string Slug,
    string Name,
    string? Description);
