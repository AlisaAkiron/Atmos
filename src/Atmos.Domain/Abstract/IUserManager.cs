using Atmos.Domain.Entities.Identity;

namespace Atmos.Domain.Abstract;

/// <summary>
/// Reads and writes user accounts and the credentials that reach them.
/// </summary>
/// <remarks>
/// Two conventions run through this interface:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>includeDetails</c> loads the social logins, WebAuthn devices and subscription
/// alongside the user. Anything that inspects or mutates those collections needs it;
/// without it they come back empty rather than lazily populated.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>noSave</c> defers the save so several changes can land in one round trip. The
/// entity is still tracked, so a later call that does save will persist it too.
/// </description>
/// </item>
/// </list>
/// </remarks>
public interface IUserManager
{
    /// <summary>
    /// Finds a user by ID.
    /// </summary>
    public Task<User?> GetUserAsync(Guid id, bool includeDetails = false);

    /// <summary>
    /// Finds the user holding an email address.
    /// </summary>
    /// <remarks>
    /// An address belongs to at most one account, which is what makes it usable as a
    /// uniqueness check before attaching one.
    /// </remarks>
    public Task<User?> GetUserByEmailAsync(string email, bool includeDetails = false);

    /// <summary>
    /// Finds the user linked to an external login.
    /// </summary>
    /// <param name="platform">Provider name the link was created under.</param>
    /// <param name="identifier">That provider's subject identifier for the account.</param>
    /// <param name="includeDetails">Also load the account's logins, devices and subscription.</param>
    public Task<User?> GetUserBySocialLoginAsync(string platform, string identifier, bool includeDetails = false);

    /// <summary>
    /// Finds the user owning a WebAuthn credential.
    /// </summary>
    /// <remarks>
    /// Credential IDs are unique across all accounts, so this doubles as the
    /// uniqueness check during registration and as the lookup during sign-in, where
    /// the assertion arrives with no hint as to who is signing in.
    /// </remarks>
    public Task<User?> GetUserByWebAuthnAsync(byte[] credentialId, bool includeDetails = false);

    /// <summary>
    /// Creates an account.
    /// </summary>
    /// <param name="userId">ID for the new account; supplied by the caller rather than generated here.</param>
    /// <param name="nickname">Display name.</param>
    /// <param name="emails">Addresses to attach, each of which must be unclaimed.</param>
    /// <param name="noSave">Defer the save to a later call.</param>
    /// <exception cref="InvalidOperationException">Any address already belongs to another account.</exception>
    public Task<User> CreateUserAsync(Guid userId, string nickname, List<string> emails, bool noSave = false);

    /// <summary>
    /// Attaches an email address to an account, doing nothing if it is already there.
    /// </summary>
    /// <exception cref="InvalidOperationException">The address already belongs to another account.</exception>
    public Task<User> AddEmailAsync(User user, string email, bool noSave = false);

    /// <summary>
    /// Links an external login to an account.
    /// </summary>
    /// <param name="user">Account to link.</param>
    /// <param name="platform">Provider name.</param>
    /// <param name="identifier">That provider's subject identifier for the account.</param>
    /// <param name="noSave">Defer the save to a later call.</param>
    public Task<User> AddSocialLoginAsync(User user, string platform, string identifier, bool noSave = false);

    /// <summary>
    /// Registers a WebAuthn credential against an account.
    /// </summary>
    /// <param name="user">Account to register against.</param>
    /// <param name="credentialId">Credential ID, unique across all accounts.</param>
    /// <param name="publicKey">Public key the assertion signature is verified against.</param>
    /// <param name="userHandle">User handle the authenticator stores, checked on assertion.</param>
    /// <param name="credType">Credential type reported by the authenticator.</param>
    /// <param name="aaGuid">Authenticator model identifier.</param>
    /// <param name="signCount">Initial signature counter.</param>
    /// <param name="noSave">Defer the save to a later call.</param>
    public Task<User> AddWebAuthnAsync(User user, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false);

    /// <summary>
    /// Resolves the account by ID, then attaches an email address to it.
    /// </summary>
    /// <exception cref="InvalidOperationException">No account has this ID, or the address is taken.</exception>
    public Task<User> AddEmailAsync(Guid userId, string email, bool noSave = false);

    /// <summary>
    /// Resolves the account by ID, then links an external login to it.
    /// </summary>
    /// <exception cref="InvalidOperationException">No account has this ID.</exception>
    public Task<User> AddSocialLoginAsync(Guid userId, string platform, string identifier, bool noSave = false);

    /// <summary>
    /// Resolves the account by ID, then registers a WebAuthn credential against it.
    /// </summary>
    /// <exception cref="InvalidOperationException">No account has this ID.</exception>
    public Task<User> AddWebAuthnAsync(Guid userId, byte[] credentialId, byte[] publicKey, byte[] userHandle, string credType, Guid aaGuid, long signCount, bool noSave = false);

    /// <summary>
    /// Stores the signature counter reported by a successful assertion.
    /// </summary>
    /// <remarks>
    /// The counter is how a cloned authenticator is detected, so it has to be written
    /// back after every sign-in. Requires a user loaded with details.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The credential is not on this user.</exception>
    public Task UpdateWebAuthnCounterAsync(User user, byte[] credentialId, long signCount, bool noSave = false);

    /// <summary>
    /// Removes one WebAuthn credential, so a lost authenticator can be revoked.
    /// </summary>
    /// <remarks>
    /// Scoped to credentials already on <paramref name="user" />, which is what stops
    /// one account from revoking another's. Requires a user loaded with details: an
    /// account loaded without them looks like it has no credentials and nothing is removed.
    /// </remarks>
    /// <returns><see langword="true"/> if the credential existed on the user.</returns>
    public Task<bool> RemoveWebAuthnAsync(User user, byte[] credentialId, bool noSave = false);

    /// <summary>
    /// Removes one external login link.
    /// </summary>
    /// <remarks>
    /// Requires a user loaded with details, for the same reason as
    /// <see cref="RemoveWebAuthnAsync" />. Callers own the decision about whether the
    /// link is safe to drop; this does not stop an account from losing its last one.
    /// </remarks>
    /// <returns><see langword="true"/> if the link existed on the user.</returns>
    public Task<bool> RemoveSocialLoginAsync(User user, Guid connectionId, bool noSave = false);
}
