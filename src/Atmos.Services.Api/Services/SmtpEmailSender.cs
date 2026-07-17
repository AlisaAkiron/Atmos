using Atmos.Services.Api.Abstract;
using Atmos.Services.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Atmos.Services.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<EmailOptions> _emailOptions;

    public SmtpEmailSender(IConfiguration configuration, IOptions<EmailOptions> emailOptions)
    {
        _configuration = configuration;
        _emailOptions = emailOptions;
    }

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("Smtp")
                               ?? throw new InvalidOperationException("Connection string 'Smtp' is not configured");

        var endpoint = ParseEndpoint(connectionString);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.Value.SenderName, _emailOptions.Value.SenderAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(endpoint.Host, endpoint.Port, SecureSocketOptions.Auto, cancellationToken);

        if (string.IsNullOrEmpty(endpoint.Username) is false)
        {
            await client.AuthenticateAsync(endpoint.Username, endpoint.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SmtpEndpoint ParseEndpoint(string connectionString)
    {
        string? username = null;
        string? password = null;
        var endpoint = connectionString;

        // Supports both a raw URI ("smtp://localhost:1025") and the key-value form
        // Aspire resources produce ("Endpoint=smtp://localhost:1025;Username=...;Password=...")
        if (connectionString.Contains('=', StringComparison.Ordinal))
        {
            var values = connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Split('=', 2))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

            endpoint = values.GetValueOrDefault("Endpoint") ?? connectionString;
            username = values.GetValueOrDefault("Username");
            password = values.GetValueOrDefault("Password");
        }

        var uri = new Uri(endpoint);
        var port = uri.IsDefaultPort ? 25 : uri.Port;

        if (string.IsNullOrEmpty(uri.UserInfo) is false)
        {
            var parts = uri.UserInfo.Split(':', 2);
            username ??= Uri.UnescapeDataString(parts[0]);
            if (parts.Length == 2)
            {
                password ??= Uri.UnescapeDataString(parts[1]);
            }
        }

        return new SmtpEndpoint(uri.Host, port, username, password);
    }

    private sealed record SmtpEndpoint(string Host, int Port, string? Username, string? Password);
}
