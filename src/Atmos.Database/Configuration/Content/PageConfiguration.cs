using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
