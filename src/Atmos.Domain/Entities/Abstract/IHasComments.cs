using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Content;

namespace Atmos.Domain.Entities.Abstract;

public interface IHasComments
{
    [Column("comments")]
    public List<Comment> Comments { get; set; }
}
