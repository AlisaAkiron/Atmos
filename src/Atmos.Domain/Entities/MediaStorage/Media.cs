using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.MediaStorage;

[Table("media")]
public record Media : IHasTimeRecord
{
    [Key]
    [Column("media_id")]
    public Guid MediaId { get; set; }

    /// <summary>Full object key in the bucket, e.g. "blog/2026/photo.webp". Folders are key prefixes.</summary>
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    /// <summary>Lowercase hex SHA-256 of the content, for duplicate detection.</summary>
    [Column("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public List<MediaReference> References { get; set; } = [];
}
