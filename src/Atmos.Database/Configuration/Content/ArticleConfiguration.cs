using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasIndex(x => x.Classification);
    }
}
