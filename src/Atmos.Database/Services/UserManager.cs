using Atmos.Domain.Abstract;
using Atmos.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Atmos.Database.Services;

public class UserManager : IUserManager
{
    private readonly AtmosDbContext _dbContext;

    public UserManager(AtmosDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var q = GetUserQueryable(includeDetails);
        if (includeDetails is false)
        {
            q.Include(x => x.SocialLogins);
        }

        return await q.FirstOrDefaultAsync(x => x.SocialLogins.Any(y => y.Platform == platform && y.Identifier == identifier));
    }

    public async Task<User?> GetUserByWebAuthnAsync(byte[] descriptorId, bool includeDetails = false)
    {
        var q = GetUserQueryable(includeDetails);
        if (includeDetails is false)
        {
            q.Include(x => x.WebAuthnDevices);
        }

        return await q.FirstOrDefaultAsync(x => x.WebAuthnDevices.Any(y => y.DescriptorId == descriptorId));
    }

    public async Task<User> CreateUserAsync(Guid userId, string nickname, List<string> emails, bool noSave = false)
    {
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

    public async Task<User> AddWebAuthnAsync(Guid userId, byte[] descriptorId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, uint signCount, bool noSave = false)
    {
        var user = await _dbContext.Users
                       .Include(x => x.WebAuthnDevices)
                       .FirstOrDefaultAsync(x => x.UserId == userId)
                   ?? throw new InvalidOperationException("User not found");

        return await AddWebAuthnAsync(user, descriptorId, publicKey, userHandle, credType, aaGuid, signCount, noSave);
    }

    public async Task<User> AddEmailAsync(User user, string email, bool noSave = false)
    {
        user.EmailAddresses.Add(email);
        _dbContext.Update(user);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> AddSocialLoginAsync(User user, string platform, string identifier, bool noSave = false)
    {
        user.SocialLogins.Add(new SocialLogin
        {
            ConnectionId = Guid.NewGuid(),
            Platform = platform,
            Identifier = identifier,
        });

        _dbContext.Update(user);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> AddWebAuthnAsync(User user, byte[] descriptorId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, uint signCount, bool noSave = false)
    {
        user.WebAuthnDevices.Add(new WebAuthn
        {
            DescriptorId = descriptorId,
            PublicKey = publicKey,
            UserHandle = userHandle,
            AaGuid = aaGuid,
            SignatureCounter = signCount,
            CredType = credType
        });

        _dbContext.Update(user);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateWebAuthnCounterAsync(User user, byte[] descriptorId, uint signCount, bool noSave = false)
    {
        user.WebAuthnDevices.First(x => x.DescriptorId == descriptorId).SignatureCounter = signCount;
        _dbContext.Update(user);

        if (noSave is false)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    private IQueryable<User> GetUserQueryable(bool includeDetails)
    {
        var queryable = _dbContext.Users.AsQueryable();

        if (includeDetails)
        {
            queryable = queryable
                .Include(x => x.SocialLogins)
                .Include(x => x.WebAuthnDevices);
        }

        return queryable;
    }
}
