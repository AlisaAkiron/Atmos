using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Abstract;

/// <summary>
/// Record the time information for an entity, like when it was created and last updated
/// </summary>
public interface IHasTimeRecord
{
    /// <summary>
    /// The time when the entity was first created
    /// </summary>
    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    /// <summary>
    /// The time when the entity was last updated
    /// </summary>
    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
