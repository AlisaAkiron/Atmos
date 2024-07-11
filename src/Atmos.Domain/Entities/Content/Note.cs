using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

public record Note : IHasDeleteRetention, IHasComments
{
    [Key]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("first_release_time")]
    public DateTimeOffset FirstReleaseTime { get; set; }

    [Column("last_edit_time")]
    public DateTimeOffset LastEditTime { get; set; }

    [Column("is_draft")]
    public bool IsDraft { get; set; }

    /// <inheritdoc />
    public List<Comment> Comments { get; set; } = [];

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeleteTime { get; set; }
}
