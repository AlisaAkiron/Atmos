using Atmos.Domain.Entities.Identity;
using Atmos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Identity;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.Property(x => x.Region)
            .HasConversion(
                v => v.Select(e => e.ToString()).ToArray(),
                v => v.Select(Enum.Parse<SubscriptionContentType>).ToList()
            );

        builder.HasIndex(x => x.Region);
    }
}
