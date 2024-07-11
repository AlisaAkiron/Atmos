using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Enums;

namespace Atmos.Domain.Entities.Identity;

[Table("subscription")]
public record Subscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("region")]
    public List<SubscriptionContentType> Region { get; set; } = [];

    [Column("user")]
    public User User { get; set; } = null!;

    [Column("user_id")]
    public Guid UserId { get; set; }
}
