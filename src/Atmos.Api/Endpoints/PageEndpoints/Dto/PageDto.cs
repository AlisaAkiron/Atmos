namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A published standalone page, as shown to visitors.
/// </summary>
/// <param name="Slug">URL segment the page is published under.</param>
/// <param name="Title">Page title.</param>
/// <param name="Content">Full page body, in Markdown.</param>
/// <param name="PublishedAt">When the page went public.</param>
public record PageDto(
    string Slug,
    string Title,
    string Content,
    DateTimeOffset PublishedAt);
