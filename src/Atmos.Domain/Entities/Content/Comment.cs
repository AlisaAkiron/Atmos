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

    [Column("children")]
    public List<Comment>? Children { get; set; }

    [Column("author_id")]
    public Guid AuthorId { get; set; }

    [Column("commentable_entity_id")]
    public string CommentableEntityId { get; set; } = string.Empty;

    [Column("commentable_entity_type")]
    public string CommentableEntityType { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeleteTime { get; set; }
}
