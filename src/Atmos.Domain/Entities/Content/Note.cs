using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

public record Note : ContentBase
{
    [Column("is_draft")]
    public bool IsDraft { get; set; }
}
