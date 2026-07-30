using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;
using Atmos.Domain.Entities.MediaStorage;

namespace Atmos.Domain.Entities.Content;

[Table("social_link")]
public record SocialLink : IHasTimeRecord
{
    [Key]
    [Column("social_link_id")]
    public Guid SocialLinkId { get; set; }

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Shown as the tooltip / accessible label in the frontend.</summary>
    [Column("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Background color of the icon circle, e.g. "#24292f".</summary>
    [Column("color")]
    public string Color { get; set; } = string.Empty;

    /// <summary>Whether the frontend applies CSS invert to the icon.</summary>
    [Column("invert")]
    public bool Invert { get; set; }

    [Column("icon_media_id")]
    public Guid IconMediaId { get; set; }

    public Media Icon { get; set; } = null!;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
