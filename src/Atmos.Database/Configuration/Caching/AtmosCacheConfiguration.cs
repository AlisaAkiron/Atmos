using Atmos.Domain.Entities.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Caching;

public class AtmosCacheConfiguration : IEntityTypeConfiguration<AtmosCache>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AtmosCache> builder)
    {
        // The background purge sweeps by expiry, and every lookup filters on it
        builder.HasIndex(x => x.ExpireAt);

        // Tag eviction runs `tags @> ARRAY[...]`, which needs GIN to stay off a seq scan
        builder.HasIndex(x => x.Tags)
            .HasMethod("GIN");
    }
}
