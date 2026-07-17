using Atmos.Domain.Entities.Identity;

namespace Atmos.Domain.Abstract;

public interface IUserManager
{
    public Task<User?> GetUserAsync(Guid id, bool includeDetails = false);

    public Task<User?> GetUserByEmailAsync(string email, bool includeDetails = false);

    public Task<User?> GetUserBySocialLoginAsync(string platform, string identifier, bool includeDetails = false);

    public Task<User?> GetUserByWebAuthnAsync(byte[] credentialId, bool includeDetails = false);

    public Task<User> CreateUserAsync(Guid userId, string nickname, List<string> emails, bool noSave = false);

    public Task<User> AddEmailAsync(User user, string email, bool noSave = false);

    public Task<User> AddSocialLoginAsync(User user, string platform, string identifier, bool noSave = false);

    public Task<User> AddWebAuthnAsync(User user, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false);

    public Task<User> AddEmailAsync(Guid userId, string email, bool noSave = false);

    public Task<User> AddSocialLoginAsync(Guid userId, string platform, string identifier, bool noSave = false);

    public Task<User> AddWebAuthnAsync(Guid userId, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false);

    public Task UpdateWebAuthnCounterAsync(User user, byte[] credentialId, long signCount, bool noSave = false);
}
