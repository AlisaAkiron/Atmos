using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Abstract;

public interface IHasDeleteRetention
{
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("delete_time")]
    public DateTimeOffset? DeleteTime { get; set; }

    public void Delete()
    {
        IsDeleted = true;
        DeleteTime = DateTimeOffset.Now;
    }
}
