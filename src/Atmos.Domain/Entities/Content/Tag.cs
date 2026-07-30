using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("tag")]
public record Tag : IHasTimeRecord
{
    [Key]
    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public List<Article> Articles { get; set; } = [];
}
