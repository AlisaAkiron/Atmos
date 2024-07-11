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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtmosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    #region Content

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<SinglePage> SinglePages => Set<SinglePage>();

    #endregion

    #region Identity

    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<User> Users => Set<User>();

    #endregion
}
