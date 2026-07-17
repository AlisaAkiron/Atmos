namespace Atmos.Services.Api.Abstract;

public interface IEmailSender
{
    public Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
