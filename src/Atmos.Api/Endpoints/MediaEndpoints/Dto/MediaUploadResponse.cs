namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// The media entry created by an upload.
/// </summary>
/// <param name="MediaId">Identifier the admin routes address this entry by.</param>
/// <param name="Key">Storage key the file was written under.</param>
/// <param name="PublicUrl">Resolved URL the file is served from.</param>
/// <param name="ContentType">MIME type the file is served with, sniffed from the key when the client sent none.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="Sha256">Lowercase hex SHA-256 of the uploaded file.</param>
/// <param name="DuplicateOfKey">Key of an existing entry with identical content, or null when the file is new. The upload still succeeds; this only reports that the earlier entry could have been reused.</param>
public record MediaUploadResponse(
    Guid MediaId,
    string Key,
    string PublicUrl,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string? DuplicateOfKey);
