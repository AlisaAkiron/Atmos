namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A tag articles can carry. An article may carry any number of them.
/// </summary>
/// <param name="Slug">URL segment, and the value the article list accepts as its tag filter.</param>
/// <param name="Name">Display name.</param>
/// <param name="ArticleCount">How many articles carry this tag.</param>
public record TagDto(
    string Slug,
    string Name,
    int ArticleCount);

/// <summary>
/// A tag as the site owner manages it, carrying the ID the public view omits.
/// </summary>
/// <param name="TagId">Identifier the admin routes address this tag by.</param>
/// <param name="Slug">URL segment, and the value the article list accepts as its tag filter.</param>
/// <param name="Name">Display name.</param>
/// <param name="ArticleCount">How many articles carry this tag. They survive its deletion, simply untagged.</param>
public record TagAdminDto(
    Guid TagId,
    string Slug,
    string Name,
    int ArticleCount);
