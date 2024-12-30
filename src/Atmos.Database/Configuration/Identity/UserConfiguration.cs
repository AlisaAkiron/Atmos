using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.EmailAddresses)
            .HasMethod("GIN");

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
