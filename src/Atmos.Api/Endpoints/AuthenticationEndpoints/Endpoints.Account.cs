using System.Buffers.Text;
using System.Security.Claims;
using Atmos.Api.Endpoints.Dto;
using Atmos.Domain.Abstract;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Models;
using Atmos.Services.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Atmos.Api.Endpoints;

public partial class AuthenticationEndpoints
{
    private static void MapAccountEndpoints(IEndpointRouteBuilder authGroup)
    {
        var accountGroup = authGroup.MapGroup("/account")
            .RequireAuthorization();

        accountGroup.MapGet("/", GetAccountAsync);
        accountGroup.MapGet("/credentials", GetCredentialsAsync);
        accountGroup.MapDelete("/credentials/social/{connectionId:guid}", RemoveSocialLoginAsync);
        accountGroup.MapDelete("/credentials/webauthn/{credentialId}", RemoveWebAuthnCredentialAsync);
    }

    [EndpointSummary("Get the signed-in account")]
    private static async Task<Results<Ok<AccountDto>, UnauthorizedHttpResult>> GetAccountAsync(
        [FromServices] ICurrentUser currentUser)
    {
        var user = await currentUser.GetUserAsync();
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new AccountDto
        {
            UserId = user.UserId,
            Nickname = user.Nickname,
            EmailAddresses = user.EmailAddresses,
            IsSiteOwner = user.IsSiteOwner,
            AuthenticationMethod = currentUser.Principal?
                .FindFirstValue(AtmosAuthenticationDefaults.AuthenticationMethodClaimType)
        });
    }

    [EndpointSummary("List credentials that can reach the account")]
    private static async Task<Results<Ok<AccountCredentialsDto>, UnauthorizedHttpResult>> GetCredentialsAsync(
        [FromServices] ICurrentUser currentUser)
    {
        var user = await currentUser.GetUserAsync(true);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new AccountCredentialsDto
        {
            SocialLogins = user.SocialLogins
                .Select(x => new SocialLoginDto
                {
                    ConnectionId = x.ConnectionId,
                    Provider = x.Platform
                })
                .OrderBy(x => x.Provider, StringComparer.Ordinal)
                .ToList(),
            WebAuthnCredentials = user.WebAuthnDevices
                .Select(x => new WebAuthnCredentialDto
                {
                    CredentialId = Base64Url.EncodeToString(x.CredentialId),
                    CredType = x.CredType,
                    AaGuid = x.AaGuid,
                    CreateAt = x.CreateAt
                })
                .OrderBy(x => x.CreateAt)
                .ToList()
        });
    }

    [EndpointSummary("Unlink an external login")]
    private static async Task<Results<NoContent, NotFound, Conflict<ErrorResponse>, UnauthorizedHttpResult>> RemoveSocialLoginAsync(
        [FromRoute(Name = "connectionId")] Guid connectionId,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IUserManager userManager)
    {
        var user = await currentUser.GetUserAsync(true);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (user.SocialLogins.Any(x => x.ConnectionId == connectionId) is false)
        {
            return TypedResults.NotFound();
        }

        if (AccountSecurity.CanUnlinkSocialLogin(user.SocialLogins.Count) is false)
        {
            return TypedResults.Conflict(
                new ErrorResponse("Cannot unlink the only external login; the account would become unreachable"));
        }

        await userManager.RemoveSocialLoginAsync(user, connectionId);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Remove a WebAuthn credential")]
    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, UnauthorizedHttpResult>> RemoveWebAuthnCredentialAsync(
        [FromRoute(Name = "credentialId")] string credentialId,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IUserManager userManager)
    {
        var user = await currentUser.GetUserAsync(true);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (TryDecodeCredentialId(credentialId, out var rawCredentialId) is false)
        {
            return TypedResults.BadRequest(new ErrorResponse("Credential ID is not valid base64url"));
        }

        // Scoped to the signed-in user's own credentials, so one account can never
        // revoke another's authenticator by guessing an ID
        var removed = await userManager.RemoveWebAuthnAsync(user, rawCredentialId);

        return removed ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static bool TryDecodeCredentialId(string value, out byte[] decoded)
    {
        decoded = [];

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var buffer = new byte[Base64Url.GetMaxDecodedLength(value.Length)];
        if (Base64Url.TryDecodeFromChars(value, buffer, out var written) is false)
        {
            return false;
        }

        decoded = buffer[..written];
        return true;
    }
}
