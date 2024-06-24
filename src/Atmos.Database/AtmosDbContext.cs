using Atmos.Database.Interceptors;
using Atmos.Domain.Entities.Content;
using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Database;

public class AtmosDbContext : DbContext
{
    public AtmosDbContext(DbContextOptions<AtmosDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(new DeleteRetentionInterceptor());

        base.OnConfiguring(optionsBuilder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtmosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    #region Content

    public DbSet<Article> Articles { get; set; }

    public DbSet<Comment> Comments { get; set; }

    #endregion

    #region Identity

    protected DbSet<User> Users { get; set; }

    #endregion
}
