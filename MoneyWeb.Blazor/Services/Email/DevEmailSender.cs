namespace MoneyWeb.Blazor.Services.Email;

/// <summary>
/// Development-only <see cref="IEmailSender"/> — no real email provider is configured yet.
/// Logs the magic link + code loudly and records them in <see cref="DevMailbox"/> for the
/// /dev/sent-mail viewer, so the login flow is fully testable locally without any provider.
/// Swap this registration out for a real provider (SMTP/SendGrid/etc.) before serving real users.
/// </summary>
public class DevEmailSender(ILogger<DevEmailSender> logger, DevMailbox mailbox, EmailDiagnostics diagnostics, IHostEnvironment env) : IEmailSender
{
    public Task SendLoginEmailAsync(string toEmail, string magicLinkUrl, string code, CancellationToken ct = default)
    {
        // A sign-in code or link in a log file is a working credential. This sender is only meant for local
        // development; if it's ever active elsewhere (e.g. production started without SMTP settings), say so loudly
        // and keep the credential out of the logs.
        if (!env.IsDevelopment())
        {
            logger.LogError("No email provider is configured (Smtp:Host is empty) — sign-in email to {Email} was NOT sent.",
                LogSafe.Email(toEmail));
            diagnostics.Record(new EmailSendAttempt(toEmail, "(no provider configured)", 0, false, "Smtp:Host is not set", 0, DateTime.UtcNow));
            return Task.CompletedTask;
        }

        logger.LogWarning(
            "==== DEV EMAIL (no real provider configured) ====\nTo: {Email}\nCode: {Code}\nMagic link: {Link}\n===================================================",
            LogSafe.Text(toEmail), code, magicLinkUrl);
        mailbox.Add(new DevSentEmail(toEmail, magicLinkUrl, code, DateTime.UtcNow));
        diagnostics.Record(new EmailSendAttempt(toEmail, "(dev capture — no SMTP)", 0, true, null, 0, DateTime.UtcNow));
        return Task.CompletedTask;
    }
}
