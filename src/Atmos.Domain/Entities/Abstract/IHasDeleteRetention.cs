using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Abstract;

/// <summary>
/// Provides functionality to handle delete retention for an entity by tracking its deletion status and time
/// </summary>
public interface IHasDeleteRetention
{
    /// <summary>
    /// Mark the object is deleted
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Records the timestamp when the entity is marked as deleted
    /// </summary>
    [Column("delete_time")]
    public DateTimeOffset? DeleteAt { get; set; }

    /// <summary>
    /// Set current object to deletion status
    /// </summary>
    public void Delete(TimeProvider timeProvider)
    {
        IsDeleted = true;
        DeleteAt = timeProvider.GetUtcNow();
    }
}
