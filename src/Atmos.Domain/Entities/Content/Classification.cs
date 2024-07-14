using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Content;

[Table("classification")]
public record Classification
{
    [Key]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
