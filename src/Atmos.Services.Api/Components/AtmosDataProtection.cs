using Atmos.Database;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Api.Components;

public static class AtmosDataProtection
{
    /// <summary>
    /// Persists the Data Protection key ring to PostgreSQL. Requires
    /// <c>ConfigureNpgsql</c>, since the ring is stored through
    /// <see cref="AtmosDbContext" />.
    /// </summary>
    /// <remarks>
    /// The key ring encrypts the authentication cookie. Left at its default it is
    /// written beside the running process — inside a container's writable layer, or
    /// nowhere at all when there is no writable home, in which case the keys are held
    /// only in memory. Either way the ring does not survive a redeploy and is not
    /// shared between instances, so every restart signs every user out and a second
    /// replica cannot read the first one's cookies. The 14-day sliding expiry on the
    /// cookie is meaningless until the ring outlives the process.
    /// <para>
    /// Keys are stored unencrypted. Protecting them needs an X.509 certificate to
    /// provision and rotate, and the same database already holds the session cache
    /// and every user row, so a reader who could decrypt the ring can already read
    /// what it protects. This is a deliberate trade-off, not an oversight: revisit it
    /// if database backups ever travel somewhere the application secrets do not.
    /// </para>
    /// </remarks>
    public static IHostApplicationBuilder ConfigureDataProtection(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddDataProtection()
            .PersistKeysToDbContext<AtmosDbContext>()
            // The discriminator defaults to the entry assembly's name, which ties the
            // ring to how the app happens to be hosted. Pinning it keeps the stored
            // keys readable if that ever changes.
            .SetApplicationName("Atmos");

        return builder;
    }
}
