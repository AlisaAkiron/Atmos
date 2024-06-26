using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Content;

public record SinglePage : ContentBase
{
    [Column("single_page_type")]
    public string SinglePageType { get; set; } = string.Empty;
}
