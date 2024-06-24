using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("article")]
public record Article : IHasDeleteRetention
{
    [Key]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("is_draft")]
    public bool IsDraft { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("classification")]
    public string Classification { get; set; } = string.Empty;

    [Column("first_release_time")]
    public DateTimeOffset FirstReleaseTime { get; set; }

    [Column("last_edit_time")]
    public DateTimeOffset LastEditTime { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeleteTime { get; set; }
}
