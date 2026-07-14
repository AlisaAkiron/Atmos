namespace Atmos.Templates.Components.Emails;

public record MagicLinkViewModel
{
    public required string Token { get; init; }
    public required string Link { get; init; }
    public required int ExpirationMinutes { get; init; }
}
