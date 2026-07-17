using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Identity;

[Table("user")]
public record User : IHasTimeRecord
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("email_addresses")]
    public List<string> EmailAddresses { get; set; } = [];

    [Column("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [Column("is_site_owner")]
    public bool IsSiteOwner { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public Subscription? Subscription { get; set; }

    public List<SocialLogin> SocialLogins { get; set; } = [];

    public List<WebAuthn> WebAuthnDevices { get; set; } = [];
}
