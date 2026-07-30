using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atmos.Services.Api;

/// <summary>
/// Output-cache policy and tag names for public content endpoints. Cached
/// responses live until an admin mutation evicts the tag (the 1 h expiry is
/// only a safety net). The default output-cache gate already skips
/// authenticated requests, so only anonymous responses are cached.
/// </summary>
public static class ContentCaching
{
    public static class Tags
    {
        public const string SocialLinks = "social-links";
        public const string FriendLinks = "friend-links";
        public const string Taxonomy = "taxonomy";
        public const string Articles = "articles";
        public const string Pages = "pages";
    }

    public static class Policies
    {
        public const string SocialLinks = "SocialLinks";
        public const string FriendLinks = "FriendLinks";
        public const string Taxonomy = "Taxonomy";
        public const string Articles = "Articles";
        public const string Pages = "Pages";
    }

    internal static IHostApplicationBuilder ConfigureContentCaching(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOutputCache(options =>
        {
            var expiry = TimeSpan.FromHours(1);

            options.AddPolicy(Policies.SocialLinks, p => p.Expire(expiry).Tag(Tags.SocialLinks));
            options.AddPolicy(Policies.FriendLinks, p => p.Expire(expiry).Tag(Tags.FriendLinks));
            options.AddPolicy(Policies.Taxonomy, p => p.Expire(expiry).Tag(Tags.Taxonomy));
            options.AddPolicy(Policies.Articles, p => p.Expire(expiry).Tag(Tags.Articles));
            options.AddPolicy(Policies.Pages, p => p.Expire(expiry).Tag(Tags.Pages));
        });

        return builder;
    }
}
