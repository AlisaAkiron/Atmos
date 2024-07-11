using Atmos.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Content;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(x => new
            {
                id = x.CommentableEntityId,
                type = x.CommentableEntityType
            })
            .HasPrincipalKey(x => new
            {
                id = x.Slug,
                type = "note"
            });
    }
}
