using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.Identity;

public class SocialLoginConfiguration : IEntityTypeConfiguration<SocialLogin>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SocialLogin> builder)
    {
        builder.HasIndex(x => new
        {
            x.Platform,
            x.Identifier
        });
    }
}
