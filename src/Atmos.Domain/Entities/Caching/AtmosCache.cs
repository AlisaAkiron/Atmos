using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Caching;

/// <summary>
/// A single key/value cache entry. Backs both <c>IDistributedCache</c> and the
/// output cache, so the cache survives restarts and is shared across instances
/// without a separate cache server.
/// </summary>
[Table("atmos_cache")]
public record AtmosCache : IHasTimeRecord
{
    /// <summary>
    /// Cache key. Output cache entries are namespaced with a prefix so tag
    /// eviction can never touch application cache entries.
    /// </summary>
    [Key]
    [Column("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Opaque payload. Callers own the encoding; nothing here interprets it.
    /// </summary>
    [Column("value")]
    public byte[] Value { get; set; } = [];

    /// <summary>
    /// When the entry stops being served. <see langword="null"/> means it never
    /// expires on its own and only goes away when removed or evicted.
    /// </summary>
    [Column("expire_at")]
    public DateTimeOffset? ExpireAt { get; set; }

    /// <summary>
    /// Hard deadline that <see cref="ExpireAt"/> can never be pushed past when a
    /// sliding window is refreshed.
    /// </summary>
    [Column("absolute_expire_at")]
    public DateTimeOffset? AbsoluteExpireAt { get; set; }

    /// <summary>
    /// Sliding window, in ticks. When set, every read pushes
    /// <see cref="ExpireAt"/> out by this much (capped at <see cref="AbsoluteExpireAt"/>).
    /// </summary>
    [Column("sliding_expiration_ticks")]
    public long? SlidingExpirationTicks { get; set; }

    /// <summary>
    /// Output cache tags used for bulk eviction. Empty for plain cache entries.
    /// </summary>
    [Column("tags")]
    public string[] Tags { get; set; } = [];

    /// <inheritdoc />
    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    /// <inheritdoc />
    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
