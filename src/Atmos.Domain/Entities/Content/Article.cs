using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("article")]
public record Article : IHasTimeRecord
{
    [Key]
    [Column("article_id")]
    public Guid ArticleId { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("summary")]
    public string? Summary { get; set; }

    /// <summary>Raw markdown; the frontend renders it.</summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Null means draft. Publishing sets it; unpublishing clears it.</summary>
    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public List<Tag> Tags { get; set; } = [];

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
