using Atmos.Domain.Entities.MediaStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.MediaStorage;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Sha256);
    }
}
