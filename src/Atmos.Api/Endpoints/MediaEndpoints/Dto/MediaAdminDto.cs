namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// One piece of content holding a reference to a media entry, which is what blocks its deletion.
/// </summary>
/// <param name="ReferrerType">Kind of content holding the reference, such as Article or SocialLink.</param>
/// <param name="ReferrerId">Identifier of that content.</param>
public record MediaReferrerDto(
    string ReferrerType,
    Guid ReferrerId);

/// <summary>
/// A stored media entry with everything needed to decide whether it is still in use.
/// </summary>
/// <param name="MediaId">Identifier the admin routes address this entry by.</param>
/// <param name="Key">Storage key, which is also the path under the public media base URL.</param>
/// <param name="PublicUrl">Resolved URL the file is served from.</param>
/// <param name="ContentType">MIME type the file is served with.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="Sha256">Lowercase hex SHA-256 of the file, which is how duplicates are spotted.</param>
/// <param name="RefCount">How many pieces of content reference this entry. Deletion needs this to be zero.</param>
/// <param name="Referrers">The referencing content, so a blocked deletion can be traced.</param>
public record MediaAdminDto(
    Guid MediaId,
    string Key,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string Sha256,
    int RefCount,
    List<MediaReferrerDto> Referrers);

/// <summary>
/// A page of the media list.
/// </summary>
/// <param name="TotalCount">Entries matching the query across every page.</param>
/// <param name="Page">Page number this response covers, counting from 1.</param>
/// <param name="PageSize">Entries per page, after clamping to the server maximum.</param>
/// <param name="Items">The entries on this page, ordered by key.</param>
public record MediaAdminListResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<MediaAdminDto> Items);
