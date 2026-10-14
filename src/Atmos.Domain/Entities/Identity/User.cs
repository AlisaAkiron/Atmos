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

    /// <summary>
    /// The account's email address, or null when the provider gave none.
    /// </summary>
    /// <remarks>
    /// One per account and unique across accounts. Not a credential and not a way in:
    /// sign-in is always through an external provider or a WebAuthn credential.
    /// </remarks>
    [Column("email_address")]
    public string? EmailAddress { get; set; }

    [Column("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [Column("is_site_owner")]
    public bool IsSiteOwner { get; set; }

    /// <summary>
    /// Changes whenever something that can reach this account is revoked.
    /// </summary>
    /// <remarks>
    /// Issued into the session cookie at sign-in and compared on every request, so
    /// moving it ends every session already handed out. Without it a cookie stays
    /// valid for its full lifetime no matter what is unlinked or removed.
    /// </remarks>
    [Column("security_stamp")]
    public Guid SecurityStamp { get; set; }

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public Subscription? Subscription { get; set; }

    public List<SocialLogin> SocialLogins { get; set; } = [];

    public List<WebAuthn> WebAuthnDevices { get; set; } = [];
}
