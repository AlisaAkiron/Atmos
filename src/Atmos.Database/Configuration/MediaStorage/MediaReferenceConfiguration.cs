using Atmos.Domain.Entities.MediaStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.MediaStorage;

public class MediaReferenceConfiguration : IEntityTypeConfiguration<MediaReference>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaReference> builder)
    {
        builder.HasIndex(x => new
        {
            x.MediaId,
            x.ReferrerType,
            x.ReferrerId
        }).IsUnique();

        // Stored as text for readable rows and stable values if enum members are reordered
        builder.Property(x => x.ReferrerType)
            .HasConversion<string>();

        builder.HasOne(x => x.Media)
            .WithMany(x => x.References)
            .HasForeignKey(x => x.MediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
