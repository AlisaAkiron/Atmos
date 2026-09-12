namespace Atmos.Api.Endpoints.Dto;

/// <summary>
/// A category articles can be filed under. An article belongs to at most one.
/// </summary>
/// <param name="Slug">URL segment, and the value the article list accepts as its category filter.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Longer blurb for the category page, or null when none is written.</param>
/// <param name="ArticleCount">How many articles are filed under this category.</param>
public record CategoryDto(
    string Slug,
    string Name,
    string? Description,
    int ArticleCount);

/// <summary>
/// A category as the site owner manages it, carrying the ID the public view omits.
/// </summary>
/// <param name="CategoryId">Identifier the admin routes address this category by.</param>
/// <param name="Slug">URL segment, and the value the article list accepts as its category filter.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Longer blurb for the category page, or null when none is written.</param>
/// <param name="ArticleCount">How many articles are filed under this category. They survive its deletion, uncategorised.</param>
public record CategoryAdminDto(
    Guid CategoryId,
    string Slug,
    string Name,
    string? Description,
    int ArticleCount);
