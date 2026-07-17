using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atmos.Api.Endpoints.Dto;
using Atmos.Common.Utils;
using Atmos.Domain.Entities.Identity;
using Atmos.Services.Api;
using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Options.Authentication;
using Atmos.Templates;
using Atmos.Templates.Components.Emails;
using Atmos.Templates.Resources.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using AuthenticationOptions = Atmos.Services.Api.Options.Authentication.AuthenticationOptions;

namespace Atmos.Api.Endpoints;

public partial class AuthenticationEndpoints
{
    private const int MagicLinkMaxAttempts = 5;

    [EndpointSummary("Send magic link to email")]
    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> SendLinkAsync(
        [FromBody] MagicLinkSendDto dto,
        HttpContext httpContext,
        [FromServices] IOptions<AuthenticationOptions> authenticationOptions,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IEmailSender emailSender,
        [FromServices] TemplateRenderer templateRenderer,
        [FromServices] IStringLocalizer<MagicLinkResources> localizer,
        [FromServices] TimeProvider timeProvider,
        [FromServices] IHostEnvironment hostEnvironment)
    {
        var options = authenticationOptions.Value.MagicLink;
        if (options.Enable is false)
        {
            return TypedResults.NotFound();
        }

        if (EmailUtils.IsValid(dto.Email) is false)
        {
            return TypedResults.BadRequest("Invalid email address");
        }

        var email = EmailUtils.Normalize(dto.Email);
        var token = RandomUtils.GetRandomAlphaNumericString(options.TokenLength);
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(options.ExpirationMinutes);

        // One active challenge per email; a new send replaces the previous token
        var challenge = new MagicLinkChallenge(HashToken(token), 0, expiresAt);
        await distributedCache.SetStringAsync(
            GetMagicLinkCacheKey(email),
            JsonSerializer.Serialize(challenge),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            });

        var link = BuildMagicLink(options, httpContext, hostEnvironment, email, token);

        var html = await templateRenderer.RenderTemplateAsync<MagicLinkComposite, MagicLinkViewModel>(
            new MagicLinkViewModel
            {
                Token = token,
                Link = link,
                ExpirationMinutes = options.ExpirationMinutes
            });

        await emailSender.SendAsync(email, localizer["Title"], html, httpContext.RequestAborted);

        return TypedResults.NoContent();
    }

    [EndpointSummary("Verify magic link from email")]
    [EndpointDescription("Target of the emailed link. Signs the user in and redirects.")]
    private static async Task<Results<RedirectHttpResult, NotFound, BadRequest<string>>> VerifyLinkAsync(
        [FromQuery(Name = "email")] string email,
        [FromQuery(Name = "token")] string token,
        HttpContext httpContext,
        [FromServices] IOptions<AuthenticationOptions> authenticationOptions,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IUserAccountService userAccountService)
    {
        var options = authenticationOptions.Value.MagicLink;
        if (options.Enable is false)
        {
            return TypedResults.NotFound();
        }

        var user = await ConsumeMagicLinkTokenAsync(distributedCache, userAccountService, email, token);
        if (user is null)
        {
            return TypedResults.BadRequest("Invalid or expired magic link");
        }

        var principal = userAccountService.CreatePrincipal(user, "magic-link");
        await httpContext.SignInAsync(AtmosAuthenticationDefaults.Scheme, principal);

        return TypedResults.Redirect(options.SignedInRedirectUrl);
    }

    [EndpointSummary("Verify magic link token")]
    [EndpointDescription("For clients that collect the emailed verification code and sign in via XHR.")]
    private static async Task<Results<SignInHttpResult, NotFound, BadRequest<string>>> VerifyTokenAsync(
        [FromBody] MagicLinkVerifyDto dto,
        [FromServices] IOptions<AuthenticationOptions> authenticationOptions,
        [FromServices] IDistributedCache distributedCache,
        [FromServices] IUserAccountService userAccountService)
    {
        var options = authenticationOptions.Value.MagicLink;
        if (options.Enable is false)
        {
            return TypedResults.NotFound();
        }

        var user = await ConsumeMagicLinkTokenAsync(distributedCache, userAccountService, dto.Email, dto.Token);
        if (user is null)
        {
            return TypedResults.BadRequest("Invalid or expired token");
        }

        var principal = userAccountService.CreatePrincipal(user, "magic-link");
        return TypedResults.SignIn(principal, new AuthenticationProperties(), AtmosAuthenticationDefaults.Scheme);
    }

    private static async Task<User?> ConsumeMagicLinkTokenAsync(
        IDistributedCache distributedCache,
        IUserAccountService userAccountService,
        string email,
        string token)
    {
        if (EmailUtils.IsValid(email) is false || string.IsNullOrEmpty(token))
        {
            return null;
        }

        var normalized = EmailUtils.Normalize(email);
        var cacheKey = GetMagicLinkCacheKey(normalized);

        var stored = await distributedCache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(stored))
        {
            return null;
        }

        var challenge = JsonSerializer.Deserialize<MagicLinkChallenge>(stored);
        if (challenge is null)
        {
            await distributedCache.RemoveAsync(cacheKey);
            return null;
        }

        var hashMatches = CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(challenge.TokenHash),
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        if (hashMatches is false)
        {
            var attempts = challenge.Attempts + 1;
            if (attempts >= MagicLinkMaxAttempts)
            {
                await distributedCache.RemoveAsync(cacheKey);
            }
            else
            {
                await distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(challenge with { Attempts = attempts }),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = challenge.ExpiresAt
                    });
            }

            return null;
        }

        // Tokens are single-use
        await distributedCache.RemoveAsync(cacheKey);

        return await userAccountService.GetOrCreateByEmailAsync(normalized);
    }

    private static string BuildMagicLink(MagicLinkOptions options, HttpContext httpContext, IHostEnvironment hostEnvironment, string email, string token)
    {
        string baseUrl;
        if (string.IsNullOrEmpty(options.LinkBaseUrl) is false)
        {
            baseUrl = options.LinkBaseUrl;
        }
        else if (hostEnvironment.IsDevelopment())
        {
            // Development-only convenience: the Host header is attacker-controlled, so a
            // request-derived link would let a forged Host exfiltrate the token of any email
            // address. Startup validation requires LinkBaseUrl in every other environment.
            baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/auth/magic-link/verify";
        }
        else
        {
            throw new InvalidOperationException("Authentication:MagicLink:LinkBaseUrl is not configured");
        }

        return $"{baseUrl}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string GetMagicLinkCacheKey(string email)
    {
        return $"atmos:magiclink:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email)))}";
    }

    private sealed record MagicLinkChallenge(string TokenHash, int Attempts, DateTimeOffset ExpiresAt);
}
