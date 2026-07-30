using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class FriendLinkConfiguration : IEntityTypeConfiguration<FriendLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FriendLink> builder)
    {
        // Media deletion is guarded in the API by the reference count;
        // Restrict makes the database enforce the same invariant.
        builder.HasOne(x => x.Avatar)
            .WithMany()
            .HasForeignKey(x => x.AvatarMediaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
