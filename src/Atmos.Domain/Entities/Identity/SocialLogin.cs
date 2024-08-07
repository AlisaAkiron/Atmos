﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atmos.Domain.Entities.Identity;

[Table("social_login")]
public record SocialLogin
{
    [Key]
    [Column("connection_id")]
    public Guid ConnectionId { get; set; }

    [Column("user")]
    public User User { get; set; } = null!;

    [Column("platform")]
    public string Platform { get; set; } = string.Empty;

    [Column("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [Column("user_id")]
    public Guid UserId { get; set; }
}
