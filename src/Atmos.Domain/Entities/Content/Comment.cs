using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;
using Atmos.Domain.Entities.Identity;

namespace Atmos.Domain.Entities.Content;

[Table("comment")]
public record Comment : IHasDeleteRetention
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("author")]
    public User Author { get; set; } = null!;

    [Column("parent")]
    public Comment? Parent { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeleteTime { get; set; }
}
