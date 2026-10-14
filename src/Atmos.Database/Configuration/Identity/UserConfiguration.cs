using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Unique, and nullable so the many accounts whose provider gave no address do
        // not collide: PostgreSQL treats NULLs as distinct in a unique index.
        builder.HasIndex(x => x.EmailAddress)
            .IsUnique();

        builder.HasMany(x => x.SocialLogins)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId);

        builder.HasMany(x => x.WebAuthnDevices)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId);

        builder.HasOne(x => x.Subscription)
            .WithOne(x => x.User)
            .HasForeignKey<Subscription>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
