using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Content;

namespace Atmos.Domain.Entities.Identity;

[Table("user")]
public record User
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

    [Column("subscription")]
    public Subscription Subscription { get; set; } = null!;

    [Column("social_logins")]
    public List<SocialLogin> SocialLogins { get; set; } = [];

    [Column("webauthn_devices")]
    public List<WebAuthn> WebAuthnDevices { get; set; } = [];
}
