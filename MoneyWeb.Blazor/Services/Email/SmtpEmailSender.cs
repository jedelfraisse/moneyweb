using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MoneyWeb.Blazor.Services.Email;

/// <summary>
/// Sends the login email via SMTP (e.g. a local dev SMTP catcher like smtp4dev, or a real
/// relay in production). Configured via the "Smtp" section: Host, Port, From, and optionally
/// Username/Password (auth) and EnableSsl.
///
/// Uses MailKit rather than System.Net.Mail.SmtpClient — the latter's EnableSsl only ever
/// implements STARTTLS (explicit TLS, the port-587 style) and has no support for implicit TLS
/// (port 465, where the connection is TLS from the first byte). Pointed at a real port-465
/// mail server, System.Net.Mail.SmtpClient tries to speak plaintext SMTP to a socket the server
/// is already expecting a TLS handshake on, and throws. MailKit picks the right mode from the
/// port automatically via SecureSocketOptions.Auto.
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

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your MoneyWeb sign-in code";
        message.Body = new TextPart("html") { Text = BuildBody(magicLinkUrl, code) };

        using var client = new SmtpClient();
        // SslOnConnect for the classic implicit-TLS port (465); otherwise let MailKit negotiate
        // STARTTLS when offered (587, or a dev catcher that doesn't support TLS at all).
        var socketOptions = enableSsl && port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
        await client.ConnectAsync(host, port, socketOptions, ct);
        if (!string.IsNullOrWhiteSpace(username))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

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
