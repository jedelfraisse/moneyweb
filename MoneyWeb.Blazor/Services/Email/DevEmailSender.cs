namespace MoneyWeb.Blazor.Services.Email;

/// <summary>
/// Development-only <see cref="IEmailSender"/> — no real email provider is configured yet.
/// Logs the magic link + code loudly and records them in <see cref="DevMailbox"/> for the
/// /dev/sent-mail viewer, so the login flow is fully testable locally without any provider.
/// Swap this registration out for a real provider (SMTP/SendGrid/etc.) before serving real users.
/// </summary>
public class DevEmailSender(ILogger<DevEmailSender> logger, DevMailbox mailbox) : IEmailSender
{
    public Task SendLoginEmailAsync(string toEmail, string magicLinkUrl, string code, CancellationToken ct = default)
    {
        logger.LogWarning(
            "==== DEV EMAIL (no real provider configured) ====\nTo: {Email}\nCode: {Code}\nMagic link: {Link}\n===================================================",
            toEmail, code, magicLinkUrl);
        mailbox.Add(new DevSentEmail(toEmail, magicLinkUrl, code, DateTime.UtcNow));
        return Task.CompletedTask;
    }
}
