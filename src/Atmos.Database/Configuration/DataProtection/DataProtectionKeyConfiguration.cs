using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atmos.Database.Configuration.DataProtection;

/// <summary>
/// Names the Data Protection key ring's table and columns.
/// </summary>
/// <remarks>
/// <see cref="DataProtectionKey" /> ships with the framework, so unlike the Atmos
/// entities it carries no <c>[Table]</c> or <c>[Column]</c> attributes and would
/// otherwise land as <c>DataProtectionKeys</c> with PascalCase columns — the one
/// table in the schema not following the convention.
/// </remarks>
public class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("data_protection_key");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FriendlyName).HasColumnName("friendly_name");
        builder.Property(x => x.Xml).HasColumnName("xml");
    }
}
