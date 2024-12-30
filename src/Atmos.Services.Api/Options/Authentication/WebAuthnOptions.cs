namespace Atmos.Services.Api.Options.Authentication;

public record WebAuthnOptions
{
    public bool Enable { get; set; }

    public string ServerDomain { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;

    public string? ServerIcon { get; set; }

    public List<string> Origins { get; set; } = [];
}
