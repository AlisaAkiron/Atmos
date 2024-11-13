using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Atmos.Services.Api.Common.Dto;

public record ArticleMetadataDto
{
    [JsonPropertyName("slug")]
    [Description("Article's slug, also as an unique identifier")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    [Description("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("first_release_time")]
    [Description("The first release time")]
    public DateTimeOffset FirstReleaseTime { get; set; }

    [JsonPropertyName("last_edit_time")]
    [Description("The last edit time")]
    public DateTimeOffset LastEditTime { get; set; }
}
