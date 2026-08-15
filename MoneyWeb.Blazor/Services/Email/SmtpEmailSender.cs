using System.Net;
using System.Net.Mail;

namespace MoneyWeb.Blazor.Services.Email;

/// <summary>
/// Sends the login email via SMTP (e.g. a local dev SMTP catcher like smtp4dev, or a real
/// relay in production). Configured via the "Smtp" section: Host, Port, From, and optionally
/// Username/Password (auth) and EnableSsl (STARTTLS).
/// </summary>
public class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendLoginEmailAsync(string toEmail, string magicLinkUrl, string code, CancellationToken ct = default)
    {
        var host = config["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(config["Smtp:Port"], out var configuredPort) ? configuredPort : 25;
        var from = config["Smtp:From"] ?? "noreply@moneyweb.local";
        var username = config["Smtp:Username"];
        var password = config["Smtp:Password"];
        var enableSsl = bool.TryParse(config["Smtp:EnableSsl"], out var ssl) && ssl;

        using var message = new MailMessage(from, toEmail)
        {
            Subject = "Your MoneyWeb sign-in code",
            Body = BuildBody(magicLinkUrl, code),
            IsBodyHtml = true,
        };

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        await client.SendMailAsync(message, ct);

        logger.LogInformation("Sent login email to {Email} via SMTP {Host}:{Port} (auth: {Auth}, ssl: {Ssl})",
            toEmail, host, port, !string.IsNullOrWhiteSpace(username), enableSsl);
    }

    private static string BuildBody(string magicLinkUrl, string code) => $"""
        <p>Your MoneyWeb sign-in code is:</p>
        <p style="font-size:1.5em; font-weight:bold; letter-spacing:0.1em">{code}</p>
        <p>Or click to sign in instantly: <a href="{magicLinkUrl}">{magicLinkUrl}</a></p>
        <p style="color:#666; font-size:0.9em">This code and link expire in 15 minutes. If you didn't request this, you can ignore this email.</p>
        """;
}
