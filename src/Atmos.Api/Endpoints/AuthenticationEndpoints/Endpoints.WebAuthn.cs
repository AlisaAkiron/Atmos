using System.ComponentModel;
using System.Text;
using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Abstract;
using Atmos.Common.Utils;
using Atmos.Domain.Abstract;
using Atmos.Domain.Entities.Identity;
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

    [EndpointSummary("WebAuthn registration")]
    private static async Task<Ok<WebAuthnAttestationDto>> AttestationAsync(
        [FromServices] IFido2 fido2,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IGuidProvider guidProvider)
    {
        User? user = null;
        if (currentUser.IsAuthenticated)
        {
            user = await currentUser.GetUserAsync(true);
        }

        Guid userId;
        bool creatingUser;
        var existingCredentials = new List<PublicKeyCredentialDescriptor>();
        var fido2User = new Fido2User();

        if (user is null)
        {
            userId = guidProvider.Create();
            creatingUser = true;

            fido2User.Name = userId.ToString();
            fido2User.DisplayName = RandomUtils.GetRandomAlphabetString(6);
            fido2User.Id = Encoding.UTF8.GetBytes(userId.ToString());
        }
        else
        {
            userId = user.UserId;
            creatingUser = false;
            existingCredentials = user.WebAuthnDevices
                .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
                .ToList();

            fido2User.Name = user.UserId.ToString();
            fido2User.DisplayName = user.Nickname;
            fido2User.Id = Encoding.UTF8.GetBytes(user.UserId.ToString());
        }

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
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
            UserId = userId,
            DisplayName = fido2User.DisplayName,
            IsCreatingNewUser = creatingUser,
            AttestationId = attestationId,
            Options = options
        });
    }

    [EndpointSummary("WebAuthn registration verification")]
    private static async Task<Results<SignInHttpResult, NoContent, BadRequest<string>>> AttestationVerifyAsync(
        [FromRoute(Name = "attestationId"), Description("Attestation ID")] Guid attestationId,
        [FromQuery(Name = "sign_in"), Description("Set to true to return SignIn result")] bool signIn,
        [FromBody] WebAuthnAttestationVerifyDto dto,
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IUserManager userManager,
        [FromServices] IUserAccountService userAccountService)
    {
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

            // Save the credential
            var user = await currentUser.GetUserAsync() ??
                       await userManager.CreateUserAsync(Guid.Parse(credential.User.Name), credential.User.DisplayName, [], true);
            await userManager.AddWebAuthnAsync(user,
                credential.Id, credential.PublicKey, credential.User.Id,
                credential.Type.ToString(), credential.AaGuid, credential.SignCount);

            if (signIn)
            {
                var principal = userAccountService.CreatePrincipal(user, "webauthn");
                return TypedResults.SignIn(principal, new AuthenticationProperties(), AtmosAuthenticationDefaults.Scheme);
            }

            return TypedResults.NoContent();
        }
        catch (Fido2VerificationException)
        {
            return TypedResults.BadRequest("Attestation verification failed");
        }
    }

    [EndpointSummary("WebAuthn assertion")]
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
