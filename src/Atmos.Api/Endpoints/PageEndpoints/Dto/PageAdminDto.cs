namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A standalone page as its author sees it, drafts included.
/// </summary>
/// <param name="PageId">Identifier the admin routes address this page by.</param>
/// <param name="Slug">URL segment the page is published under.</param>
/// <param name="Title">Page title.</param>
/// <param name="Content">Full page body, in Markdown.</param>
/// <param name="PublishedAt">When the page went public, or null while it is still a draft.</param>
/// <param name="CreateAt">When the page was first created.</param>
/// <param name="UpdateAt">When the page was last edited.</param>
public record PageAdminDto(
    Guid PageId,
    string Slug,
    string Title,
    string Content,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreateAt,
    DateTimeOffset UpdateAt);

/// <summary>
/// A page of the admin page list.
/// </summary>
/// <param name="TotalCount">Pages across every page of results.</param>
/// <param name="Page">Page number this response covers, counting from 1.</param>
/// <param name="PageSize">Pages per page, after clamping to the server maximum.</param>
/// <param name="Items">The pages in this response, ordered by slug.</param>
public record PageAdminListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<PageAdminDto> Items);
