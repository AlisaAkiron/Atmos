using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("page")]
public record Page : IHasTimeRecord
{
    [Key]
    [Column("page_id")]
    public Guid PageId { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Raw markdown; the frontend renders it.</summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Null means draft. Publishing sets it; unpublishing clears it.</summary>
    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
