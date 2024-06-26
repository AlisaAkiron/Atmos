using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Abstract;

public abstract record ContentBase : IHasDeleteRetention
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

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeleteTime { get; set; }
}
