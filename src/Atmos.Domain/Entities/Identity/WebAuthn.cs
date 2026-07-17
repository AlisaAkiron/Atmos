using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atmos.Domain.Entities.Abstract;

namespace Atmos.Domain.Entities.Identity;

[Table("webauthn")]
public record WebAuthn : IHasTimeRecord
{
    [Key]
    [Column("credential_id")]
    public byte[] CredentialId { get; set; } = [];

    [Column("public_key")]
    public byte[] PublicKey { get; set; } = [];

    [Column("user_handle")]
    public byte[] UserHandle { get; set; } = [];

    [Column("aa_guid")]
    public Guid AaGuid { get; set; }

    [Column("signature_counter")]
    public long SignatureCounter { get; set; }

    [Column("cred_type")]
    public string CredType { get; set; } = string.Empty;

    [Column("create_at")]
    public DateTimeOffset CreateAt { get; set; }

    [Column("update_at")]
    public DateTimeOffset UpdateAt { get; set; }

    public User User { get; set; } = null!;

    [Column("user_id")]
    public Guid UserId { get; set; }
}
