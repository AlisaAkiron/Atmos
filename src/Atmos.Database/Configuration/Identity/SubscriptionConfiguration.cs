using System.Linq.Expressions;
using Atmos.Domain.Entities.Identity;
using Atmos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Identity;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        var valueComparer = new ValueComparer<List<SubscriptionContentType>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(x => x.Region)
            .HasConversion(
                v => v.Select(e => e.ToString()).ToArray(),
                v => v.Select(Enum.Parse<SubscriptionContentType>).ToList(),
                valueComparer
            );

        builder.HasIndex(x => x.Region);
    }
}
