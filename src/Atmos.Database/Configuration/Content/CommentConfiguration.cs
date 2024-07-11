using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public record CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder
            .HasDiscriminator(x => x.CommentableEntityType)
            .HasValue<Article>("article")
            .HasValue<Note>("note")
            .HasValue<SinglePage>("single-page");
    }
}
