using Atmos.Common.Abstract;
using Atmos.Database;
using Atmos.Domain.Entities.MediaStorage;
using Atmos.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Services.Media.Services;

/// <summary>
/// Maintains media_reference rows for one referrer at a time. Methods only
/// mutate the change tracker; the caller owns SaveChangesAsync so content
/// and reference changes commit in one transaction.
/// </summary>
public class MediaReferenceService
{
    private readonly AtmosDbContext _dbContext;
    private readonly IGuidProvider _guidProvider;
    private readonly MediaUrlBuilder _urlBuilder;

    public MediaReferenceService(AtmosDbContext dbContext, IGuidProvider guidProvider, MediaUrlBuilder urlBuilder)
    {
        _dbContext = dbContext;
        _guidProvider = guidProvider;
        _urlBuilder = urlBuilder;
    }

    public async Task SetMarkdownReferencesAsync(MediaReferrerType referrerType, Guid referrerId, string markdown,
        CancellationToken ct = default)
    {
        // Materialized as a List so the Contains below always translates to SQL
        var keys = MediaReferenceExtractor.ExtractKeys(markdown, _urlBuilder.PublicBaseUrl).ToList();

        // Unknown keys (typos, deleted media) are silently ignored: a bad URL
        // in markdown is not a save error, it just isn't a reference.
        var mediaIds = await _dbContext.Media
            .Where(x => keys.Contains(x.Key))
            .Select(x => x.MediaId)
            .ToListAsync(ct);

        await SetReferencesAsync(referrerType, referrerId, mediaIds, ct);
    }

    public async Task SetReferencesAsync(MediaReferrerType referrerType, Guid referrerId,
        IReadOnlyCollection<Guid> mediaIds, CancellationToken ct = default)
    {
        var existing = await _dbContext.MediaReferences
            .Where(x => x.ReferrerType == referrerType && x.ReferrerId == referrerId)
            .ToListAsync(ct);

        var wanted = mediaIds.ToHashSet();

        var stale = existing.Where(x => wanted.Contains(x.MediaId) is false).ToList();
        _dbContext.MediaReferences.RemoveRange(stale);

        var present = existing.Select(x => x.MediaId).ToHashSet();
        var missing = wanted.Where(x => present.Contains(x) is false);

        foreach (var mediaId in missing)
        {
            await _dbContext.MediaReferences.AddAsync(new MediaReference
            {
                MediaReferenceId = _guidProvider.Create(),
                MediaId = mediaId,
                ReferrerType = referrerType,
                ReferrerId = referrerId
            }, ct);
        }
    }

    public async Task ClearReferencesAsync(MediaReferrerType referrerType, Guid referrerId,
        CancellationToken ct = default)
    {
        var existing = await _dbContext.MediaReferences
            .Where(x => x.ReferrerType == referrerType && x.ReferrerId == referrerId)
            .ToListAsync(ct);

        _dbContext.MediaReferences.RemoveRange(existing);
    }
}
