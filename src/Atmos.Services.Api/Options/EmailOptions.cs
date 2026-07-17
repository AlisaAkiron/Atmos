namespace Atmos.Services.Api.Options;

public record EmailOptions
{
    public string SenderAddress { get; set; } = "atmos@localhost";

    public string SenderName { get; set; } = "Atmos";
}
