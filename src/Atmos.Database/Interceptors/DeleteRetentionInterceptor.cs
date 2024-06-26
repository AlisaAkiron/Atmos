using Atmos.Domain.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Atmos.Database.Interceptors;

public class DeleteRetentionInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return result;
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry is not { State: EntityState.Deleted, Entity: IHasDeleteRetention dr })
            {
                return result;
            }

            entry.State = EntityState.Modified;
            dr.IsDeleted = true;
            dr.DeleteTime = DateTimeOffset.UtcNow;
        }

        return result;
    }
}
