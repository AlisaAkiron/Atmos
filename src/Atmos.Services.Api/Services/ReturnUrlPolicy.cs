namespace Atmos.Services.Api.Services;

/// <summary>
/// Decides where a completed sign-in may send the browser.
/// </summary>
/// <remarks>
/// This is the open-redirect guard. Sign-in is the one flow that hands an
/// attacker-supplied URL straight to <c>Response.Redirect</c>, and it runs at the
/// moment the session cookie exists, so anything that widens it turns a phishing
/// link into an authenticated landing on someone else's page.
/// </remarks>
public static class ReturnUrlPolicy
{
    /// <summary>
    /// Whether <paramref name="returnUrl" /> may be redirected to.
    /// </summary>
    /// <param name="returnUrl">The requested destination.</param>
    /// <param name="allowedOrigins">
    /// Origins a separate-origin frontend may be returned to. Empty permits local
    /// paths only, which is the same-origin deployment and the default.
    /// </param>
    public static bool IsAllowed(string? returnUrl, IReadOnlyCollection<string> allowedOrigins)
    {
        ArgumentNullException.ThrowIfNull(allowedOrigins);

        if (string.IsNullOrEmpty(returnUrl))
        {
            return false;
        }

        if (IsLocalPath(returnUrl))
        {
            return true;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var target) is false)
        {
            return false;
        }

        // Rejected before any comparison: "javascript:" and "data:" carry no origin to
        // compare, and are the payload an open redirect is usually wanted for
        if (IsHttp(target) is false)
        {
            return false;
        }

        foreach (var origin in allowedOrigins)
        {
            if (Uri.TryCreate(origin, UriKind.Absolute, out var allowed) is false)
            {
                // A malformed entry matches nothing. That fails closed - the redirect is
                // refused with a 400 rather than quietly widened.
                continue;
            }

            if (IsSameOrigin(target, allowed))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLocalPath(string url)
    {
        if (url[0] != '/')
        {
            return false;
        }

        // "//host" and "/\host" are read as protocol-relative absolute URLs by browsers,
        // so they leave the origin despite starting with a slash
        return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
    }

    /// <remarks>
    /// Compared as parsed origins rather than as text. A prefix test would accept
    /// <c>https://good.example.evil.test</c> for an allowlisted
    /// <c>https://good.example</c>, and <see cref="Uri.Authority" /> excludes userinfo,
    /// so <c>https://good.example@evil.test</c> is correctly read as <c>evil.test</c>.
    /// </remarks>
    private static bool IsSameOrigin(Uri target, Uri allowed)
    {
        return string.Equals(target.Scheme, allowed.Scheme, StringComparison.OrdinalIgnoreCase)
               && string.Equals(target.Authority, allowed.Authority, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttp(Uri uri)
    {
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
               || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);
    }
}
