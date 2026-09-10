using Atmos.Domain.Entities.Abstract;
using Atmos.Domain.Entities.Caching;
using Atmos.Domain.Entities.Content;
using Atmos.Domain.Entities.Identity;
using Atmos.Domain.Entities.MediaStorage;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Database;

public class AtmosDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    public AtmosDbContext(DbContextOptions<AtmosDbContext> options, TimeProvider? timeProvider = null) : base(options)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtmosDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimeRecords();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimeRecords();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void StampTimeRecords()
    {
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries<IHasTimeRecord>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateAt = now;
                    entry.Entity.UpdateAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateAt = now;
                    break;
                default:
                    break;
            }
        }
    }

    #region Identity

    public DbSet<SocialLogin> SocialLogins => Set<SocialLogin>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WebAuthn> WebAuthn => Set<WebAuthn>();

    #endregion

    #region Content

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FriendLink> FriendLinks => Set<FriendLink>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<SocialLink> SocialLinks => Set<SocialLink>();
    public DbSet<Tag> Tags => Set<Tag>();

    #endregion

    #region Media

    public DbSet<Media> Media => Set<Media>();
    public DbSet<MediaReference> MediaReferences => Set<MediaReference>();

    #endregion

    #region Caching

    public DbSet<AtmosCache> AtmosCaches => Set<AtmosCache>();

    #endregion
}
