using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

[Table("category")]
public record Category : IHasTimeRecord
{
    [Key]
    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public List<Article> Articles { get; set; } = [];
}
