using Atmos.Common.Abstract;
using Atmos.Domain.Abstract;
using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Database.Services;

public class UserManager : IUserManager
{
    private readonly AtmosDbContext _dbContext;
    private readonly IGuidProvider _guidProvider;

    public UserManager(AtmosDbContext dbContext, IGuidProvider guidProvider)
    {
        _dbContext = dbContext;
        _guidProvider = guidProvider;
    }

    public async Task<User?> GetUserAsync(Guid id, bool includeDetails = false)
    {
        return await GetUserQueryable(includeDetails).FirstOrDefaultAsync(x => x.UserId == id);
    }

    public async Task<User?> GetUserByEmailAsync(string email, bool includeDetails = false)
    {
        return await GetUserQueryable(includeDetails).FirstOrDefaultAsync(x => x.EmailAddresses.Contains(email));
    }

    public async Task<User?> GetUserBySocialLoginAsync(string platform, string identifier, bool includeDetails = false)
    {
        return await GetUserQueryable(includeDetails)
            .FirstOrDefaultAsync(x => x.SocialLogins.Any(y => y.Platform == platform && y.Identifier == identifier));
    }

    public async Task<User?> GetUserByWebAuthnAsync(byte[] credentialId, bool includeDetails = false)
    {
        return await GetUserQueryable(includeDetails)
            .FirstOrDefaultAsync(x => x.WebAuthnDevices.Any(y => y.CredentialId == credentialId));
    }

    public async Task<User> CreateUserAsync(Guid userId, string nickname, List<string> emails, bool noSave = false)
    {
        foreach (var email in emails)
        {
            await EnsureEmailIsFreeAsync(email);
        }

        var user = new User
        {
            UserId = userId,
            EmailAddresses = emails,
            Nickname = nickname,
        };

        await _dbContext.Users.AddAsync(user);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> AddEmailAsync(Guid userId, string email, bool noSave = false)
    {
        var user = await GetUserAsync(userId) ?? throw new InvalidOperationException("User not found");

        return await AddEmailAsync(user, email, noSave);
    }

    public async Task<User> AddSocialLoginAsync(Guid userId, string platform, string identifier, bool noSave = false)
    {
        var user = await _dbContext.Users
                       .Include(x => x.SocialLogins)
                       .FirstOrDefaultAsync(x => x.UserId == userId)
                   ?? throw new InvalidOperationException("User not found");

        return await AddSocialLoginAsync(user, platform, identifier, noSave);
    }

    public async Task<User> AddWebAuthnAsync(Guid userId, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false)
    {
        var user = await _dbContext.Users
                       .Include(x => x.WebAuthnDevices)
                       .FirstOrDefaultAsync(x => x.UserId == userId)
                   ?? throw new InvalidOperationException("User not found");

        return await AddWebAuthnAsync(user, credentialId, publicKey, userHandle, credType, aaGuid, signCount, noSave);
    }

    public async Task<User> AddEmailAsync(User user, string email, bool noSave = false)
    {
        if (user.EmailAddresses.Contains(email))
        {
            return user;
        }

        await EnsureEmailIsFreeAsync(email);

        user.EmailAddresses.Add(email);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> AddSocialLoginAsync(User user, string platform, string identifier, bool noSave = false)
    {
        // Child entities are added explicitly: DbContext.Update(user) would mark entities with
        // a pre-set key as Modified, turning the intended INSERT into a failing UPDATE
        var socialLogin = new SocialLogin
        {
            ConnectionId = _guidProvider.Create(),
            Platform = platform,
            Identifier = identifier,
            UserId = user.UserId
        };

        user.SocialLogins.Add(socialLogin);
        await _dbContext.SocialLogins.AddAsync(socialLogin);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> AddWebAuthnAsync(User user, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false)
    {
        var device = new WebAuthn
        {
            CredentialId = credentialId,
            PublicKey = publicKey,
            UserHandle = userHandle,
            AaGuid = aaGuid,
            SignatureCounter = signCount,
            CredType = credType,
            UserId = user.UserId
        };

        user.WebAuthnDevices.Add(device);
        await _dbContext.WebAuthn.AddAsync(device);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateWebAuthnCounterAsync(User user, byte[] credentialId, long signCount, bool noSave = false)
    {
        var device = user.WebAuthnDevices.FirstOrDefault(x => x.CredentialId.SequenceEqual(credentialId))
                     ?? throw new InvalidOperationException("WebAuthn credential not found on user");

        device.SignatureCounter = signCount;

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task EnsureEmailIsFreeAsync(string email)
    {
        var existing = await GetUserByEmailAsync(email);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Email '{email}' is already associated with another user");
        }
    }

    private IQueryable<User> GetUserQueryable(bool includeDetails)
    {
        var queryable = _dbContext.Users.AsQueryable();

        if (includeDetails)
        {
            queryable = queryable
                .Include(x => x.SocialLogins)
                .Include(x => x.WebAuthnDevices)
                .Include(x => x.Subscription);
        }

        return queryable;
    }
}
