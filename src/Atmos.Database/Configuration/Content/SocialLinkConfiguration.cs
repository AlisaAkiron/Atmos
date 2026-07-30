using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class SocialLinkConfiguration : IEntityTypeConfiguration<SocialLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SocialLink> builder)
    {
        builder.HasOne(x => x.Icon)
            .WithMany()
            .HasForeignKey(x => x.IconMediaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
