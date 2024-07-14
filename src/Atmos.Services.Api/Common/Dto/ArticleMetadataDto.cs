using System.Text.Json.Serialization;

namespace Atmos.Services.Api.Common.Dto;

public record ArticleMetadataDto
{
    /// <summary>
    ///     Article's slug, also as an unique identifier
    /// </summary>
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     Title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     The first release time
    /// </summary>
    [JsonPropertyName("first_release_time")]
    public DateTimeOffset FirstReleaseTime { get; set; }

    /// <summary>
    ///     The last edit time
    /// </summary>
    [JsonPropertyName("last_edit_time")]
    public DateTimeOffset LastEditTime { get; set; }
}
