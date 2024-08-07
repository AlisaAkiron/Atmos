﻿using System.Text.Json.Serialization;

namespace Atmos.Services.Api.Common.Dto;

public record ArticleDto
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
    ///     Summary, generated by AI
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    ///     Main article content
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

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

    /// <summary>
    ///     Article classification
    /// </summary>
    [JsonPropertyName("classification")]
    public string Classification { get; set; } = string.Empty;
}
