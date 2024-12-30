using System.ComponentModel;
using System.Security.Claims;
using System.Text;
using Atmos.Api.Endpoints.AuthenticationEndpoints.Dto;
using Atmos.Common.Utils;
using Atmos.Database;
using Atmos.Domain.Abstract;
using Atmos.Services.Api.Abstract;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Atmos.Api.Endpoints.AuthenticationEndpoints;

public partial class Endpoints
{
    [EndpointSummary("WebAuthn registration")]
    private static async Task<Ok<WebAuthnAttestationDto>> AttestationAsync(
        [FromServices] IFido2 fido2,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IDistributedCache distributedCache)
    {
        Guid? userId = null;
        var creatingUser = false;
        var existingCredentials = new List<PublicKeyCredentialDescriptor>();

        var fido2User = new Fido2User();

        if (currentUser.Principal?.Identity?.IsAuthenticated is not true)
        {
            var userDisplayName = RandomUtils.GetRandomAlphabetString(6);

            userId = Guid.NewGuid();
            creatingUser = true;

            fido2User.Name = userId.ToString();
            fido2User.DisplayName = userDisplayName;
            fido2User.Id = Encoding.UTF8.GetBytes(userId.ToString()!);
        }
        else
        {
            var user = await currentUser.GetUserAsync(true);
            if (user is not null)
            {
                userId = user.UserId;
                creatingUser = false;
                existingCredentials = user.WebAuthnDevices
                    .Select(x => new PublicKeyCredentialDescriptor(x.DescriptorId))
                    .ToList();

                fido2User.Name = user.UserId.ToString();
                fido2User.DisplayName = user.Nickname;
                fido2User.Id = Encoding.UTF8.GetBytes(user.UserId.ToString());
            }
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

        var attestationId = Guid.NewGuid();
        var cacheKey = GetAttestationChallengeCacheKey(attestationId);
        await distributedCache.SetStringAsync(cacheKey, options.ToJson());

        return TypedResults.Ok(new WebAuthnAttestationDto
        {
            UserId = userId!.Value,
            DisplayName = fido2User.DisplayName,
            IsCreatingNewUser = creatingUser,
            AttestationId = attestationId,
            Options = options
        });
    }

    [EndpointSummary("WebAuthn registration verification")]
    private static async Task<Results<SignInHttpResult, NoContent, BadRequest<string>>> AttestationVerifyAsync(
        [FromRoute(Name = "attestationId"), Description("Attestation ID")] Guid attestationId,
        [FromQuery(Name = "sign_in"), Description("Set to true to return SignIn reuslt")] bool signIn,
        [FromBody] WebAuthnAttestationVerifyDto dto,
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] AtmosDbContext dbContext,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IUserManager userManager)
    {
        // Find the attestation
        var cacheKey = GetAttestationChallengeCacheKey(attestationId);
        var options = await distributedCache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(options))
        {
            return TypedResults.BadRequest("Invalid attestation ID");
        }

        // Build CredentialCreateOptions
        var credentialCreateOptions = CredentialCreateOptions.FromJson(options);

        // Verify and make the credentials
        var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = dto.AttestationResponse,
            OriginalOptions = credentialCreateOptions,
            IsCredentialIdUniqueToUserCallback = async (p, token) =>
            {
                var exist = await dbContext.WebAuthn.AnyAsync(x => x.DescriptorId == p.CredentialId, token);
                return exist;
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
            // TODO: Unified claim creation
            return TypedResults.SignIn(new ClaimsPrincipal(), new AuthenticationProperties(), "");
        }

        return TypedResults.NoContent();
    }

    [EndpointSummary("WebAuthn assertion")]
    private static async Task<Ok<WebAuthnAssertionDto>> AssertionAsync(
        [FromServices] IFido2 fido2,
        [FromServices] IDistributedCache distributedCache)
    {
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Required
        });

        var challengeId = Guid.NewGuid();
        var cacheKey = GetAssertionChallengeCacheKey(challengeId);

        await distributedCache.SetStringAsync(cacheKey, options.ToJson());

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

        // Build AssertionOptions
        var assertionOptions = AssertionOptions.FromJson(options);

        // Find stored credential
        var user = await userManager.GetUserByWebAuthnAsync(dto.AssertionResponse.Id);
        if (user is null)
        {
            return TypedResults.BadRequest("Invalid credential ID");
        }
        var storedCredential = user.WebAuthnDevices
            .First(x => x.DescriptorId.SequenceEqual(dto.AssertionResponse.Id));

        // Verify the assertion
        var verifyAssertionResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = dto.AssertionResponse,
            OriginalOptions = assertionOptions,
            StoredPublicKey = storedCredential.PublicKey,
            StoredSignatureCounter = 0,
            IsUserHandleOwnerOfCredentialIdCallback =  (p, _) =>
                Task.FromResult(p.CredentialId.SequenceEqual(storedCredential.DescriptorId) &&
                                p.UserHandle.SequenceEqual(storedCredential.UserHandle))
        });

        await userManager.UpdateWebAuthnCounterAsync(user, verifyAssertionResult.CredentialId, verifyAssertionResult.SignCount);

        // TODO: Unified claim creation
        return TypedResults.SignIn(new ClaimsPrincipal(), new AuthenticationProperties(), "");
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
