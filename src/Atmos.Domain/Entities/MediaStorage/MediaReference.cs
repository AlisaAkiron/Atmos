using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Enums;

namespace Atmos.Domain.Entities.MediaStorage;

[Table("media_reference")]
public record MediaReference
{
    [Key]
    [Column("media_reference_id")]
    public Guid MediaReferenceId { get; set; }

    [Column("media_id")]
    public Guid MediaId { get; set; }

    public Media Media { get; set; } = null!;

    [Column("referrer_type")]
    public MediaReferrerType ReferrerType { get; set; }

    /// <summary>PK of the referring entity; no FK — the referrer is polymorphic.</summary>
    [Column("referrer_id")]
    public Guid ReferrerId { get; set; }
}
