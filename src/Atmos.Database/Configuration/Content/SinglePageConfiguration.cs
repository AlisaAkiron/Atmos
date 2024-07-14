using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class SinglePageConfiguration : IEntityTypeConfiguration<SinglePage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SinglePage> builder)
    {
        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(x => new
            {
                x.CommentableEntityId, x.CommentableEntityType
            })
            .HasPrincipalKey(x => new
            {
                x.Slug, x.ContentType
            });
    }
}
