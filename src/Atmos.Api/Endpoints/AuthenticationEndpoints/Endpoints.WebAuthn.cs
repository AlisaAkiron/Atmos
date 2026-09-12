using System.ComponentModel;
using System.Text;
using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
using Atmos.Domain.Abstract;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Atmos.Api.Endpoints;

public partial class AuthenticationEndpoints
{
    private static readonly TimeSpan WebAuthnChallengeLifetime = TimeSpan.FromMinutes(5);

    [EndpointSummary("Register a WebAuthn credential for the signed-in account")]
    [EndpointDescription(
        "Begins registering an authenticator for the signed-in account. Returns the options to hand to " +
        "navigator.credentials.create together with the attestation ID identifying the ceremony; both expire after " +
        "five minutes."
    )]
    private static async Task<Results<Ok<WebAuthnAttestationDto>, UnauthorizedHttpResult>> AttestationAsync(
        [FromServices] IFido2 fido2,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IGuidProvider guidProvider)
    {
        // WebAuthn adds a credential to an account that already exists; accounts are
        // only ever created by an external identity provider. The route requires
        // authorization, so a missing row here means the account went away mid-session.
        var user = await currentUser.GetUserAsync(true);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var existingCredentials = user.WebAuthnDevices
            .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
            .ToList();

        var fido2User = new Fido2User
        {
            Name = user.UserId.ToString(),
            DisplayName = user.Nickname,
            Id = Encoding.UTF8.GetBytes(user.UserId.ToString())
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = existingCredentials,
            // Sign-in asks for an assertion with no credential hint and demands user
            // verification, so the credential has to be discoverable and UV-capable.
            // AuthenticatorSelection.Default discourages both, which would register
            // credentials that can never complete a sign-in.
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                CredProps = true
            }
        });

        var attestationId = guidProvider.Create();
        var cacheKey = GetAttestationChallengeCacheKey(attestationId);
        await distributedCache.SetStringAsync(cacheKey, options.ToJson(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = WebAuthnChallengeLifetime
        });

        return TypedResults.Ok(new WebAuthnAttestationDto
        {
            UserId = user.UserId,
            DisplayName = fido2User.DisplayName,
            AttestationId = attestationId,
            Options = options
        });
    }

    [EndpointSummary("Verify and store a WebAuthn credential")]
    [EndpointDescription(
        "Completes registration by verifying the authenticator's response and storing the credential. The challenge " +
        "is single-use, and is only accepted from the account it was issued to."
    )]
    private static async Task<Results<NoContent, UnauthorizedHttpResult, BadRequest<string>>> AttestationVerifyAsync(
        [FromRoute(Name = "attestationId"), Description("Attestation ID")] Guid attestationId,
        [FromBody] WebAuthnAttestationVerifyDto dto,
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IUserManager userManager)
    {
        var user = await currentUser.GetUserAsync(true);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        // Find the attestation
        var cacheKey = GetAttestationChallengeCacheKey(attestationId);
        var options = await distributedCache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(options))
        {
            return TypedResults.BadRequest("Invalid attestation ID");
        }

        // Challenges are single-use: remove before verification so neither a failed
        // nor a successful attempt can ever replay the same challenge
        await distributedCache.RemoveAsync(cacheKey);

        // Build CredentialCreateOptions
        var credentialCreateOptions = CredentialCreateOptions.FromJson(options);

        // The challenge was issued to whoever was signed in at the time; refuse it for
        // anyone else so a captured attestation cannot plant a credential on another account
        if (string.Equals(credentialCreateOptions.User.Name, user.UserId.ToString(), StringComparison.Ordinal) is false)
        {
            return TypedResults.BadRequest("Invalid attestation ID");
        }

        try
        {
            // Verify and make the credentials
            var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = dto.AttestationResponse,
                OriginalOptions = credentialCreateOptions,
                IsCredentialIdUniqueToUserCallback = async (p, _) =>
                {
                    var existing = await userManager.GetUserByWebAuthnAsync(p.CredentialId);
                    return existing is null;
                }
            });

            await userManager.AddWebAuthnAsync(user,
                credential.Id, credential.PublicKey, credential.User.Id,
                credential.Type.ToString(), credential.AaGuid, credential.SignCount);

            return TypedResults.NoContent();
        }
        catch (Fido2VerificationException)
        {
            return TypedResults.BadRequest("Attestation verification failed");
        }
    }

    [EndpointSummary("WebAuthn assertion")]
    [EndpointDescription(
        "Begins a WebAuthn sign-in. Returns the options to hand to navigator.credentials.get and the challenge ID " +
        "identifying the ceremony. No credential hint is sent, so the authenticator offers its own discoverable " +
        "credentials."
    )]
    private static async Task<Ok<WebAuthnAssertionDto>> AssertionAsync(
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IGuidProvider guidProvider)
    {
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Required
        });

        var challengeId = guidProvider.Create();
        var cacheKey = GetAssertionChallengeCacheKey(challengeId);

        await distributedCache.SetStringAsync(cacheKey, options.ToJson(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = WebAuthnChallengeLifetime
        });

        var dto = new WebAuthnAssertionDto
        {
            Options = options,
            ChallengeId = challengeId
        };

        return TypedResults.Ok(dto);
    }

    [EndpointSummary("WebAuthn assertion verification")]
    [EndpointDescription(
        "Completes a WebAuthn sign-in by verifying the authenticator's response, issuing the session cookie on " +
        "success. The challenge is single-use."
    )]
    private static async Task<Results<SignInHttpResult, BadRequest<string>>> AssertionVerifyAsync(
        [FromRoute(Name = "challengeId")] Guid challengeId,
        [FromBody] WebAuthnAssertionVerifyDto dto,
        [FromServices] IUserManager userManager,
        [FromServices] IUserAccountService userAccountService,
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache)
    {
        // Find the challenge
        var cacheKey = GetAssertionChallengeCacheKey(challengeId);
        var options = await distributedCache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(options))
        {
            return TypedResults.BadRequest("Invalid challenge ID");
        }

        // Challenges are single-use
        await distributedCache.RemoveAsync(cacheKey);

        // Build AssertionOptions
        var assertionOptions = AssertionOptions.FromJson(options);

        // Find stored credential
        var user = await userManager.GetUserByWebAuthnAsync(dto.AssertionResponse.RawId, true);
        if (user is null)
        {
            return TypedResults.BadRequest("Invalid credential ID");
        }
        var storedCredential = user.WebAuthnDevices
            .First(x => x.CredentialId.SequenceEqual(dto.AssertionResponse.RawId));

        try
        {
            // Verify the assertion
            var verifyAssertionResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = dto.AssertionResponse,
                OriginalOptions = assertionOptions,
                StoredPublicKey = storedCredential.PublicKey,
                StoredSignatureCounter = (uint)storedCredential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = (p, _) =>
                    Task.FromResult(p.CredentialId.SequenceEqual(storedCredential.CredentialId) &&
                                    p.UserHandle.SequenceEqual(storedCredential.UserHandle))
            });

            await userManager.UpdateWebAuthnCounterAsync(user, verifyAssertionResult.CredentialId, verifyAssertionResult.SignCount);
        }
        catch (Fido2VerificationException)
        {
            return TypedResults.BadRequest("Assertion verification failed");
        }

        var principal = userAccountService.CreatePrincipal(user, "webauthn");
        return TypedResults.SignIn(principal, new AuthenticationProperties(), AtmosAuthenticationDefaults.Scheme);
    }

    private static string GetAttestationChallengeCacheKey(Guid id)
    {
        return $"atmos:webauthn:attestation:{id}";
    }

    private static string GetAssertionChallengeCacheKey(Guid id)
    {
        return $"atmos:webauthn:assertion:{id}";
    }
}
