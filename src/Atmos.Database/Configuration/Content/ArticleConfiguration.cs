using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.PublishedAt);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Tags)
            .WithMany(x => x.Articles)
            .UsingEntity<Dictionary<string, object>>(
                "article_tag",
                right => right.HasOne<Tag>().WithMany().HasForeignKey("tag_id"),
                left => left.HasOne<Article>().WithMany().HasForeignKey("article_id"));
    }
}
