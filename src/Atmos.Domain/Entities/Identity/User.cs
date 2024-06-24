using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Identity;

public record User
{
    [Key]
    [Column("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    [Column("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [Column("is_site_owner")]
    public bool IsSiteOwner { get; set; }
}
