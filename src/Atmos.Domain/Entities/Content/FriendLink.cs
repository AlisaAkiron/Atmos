using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;
using Atmos.Domain.Entities.MediaStorage;

namespace Atmos.Domain.Entities.Content;

[Table("friend_link")]
public record FriendLink : IHasTimeRecord
{
    [Key]
    [Column("friend_link_id")]
    public Guid FriendLinkId { get; set; }

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("avatar_media_id")]
    public Guid? AvatarMediaId { get; set; }

    public Media? Avatar { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }
}
