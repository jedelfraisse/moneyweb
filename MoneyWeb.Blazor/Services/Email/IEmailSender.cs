namespace MoneyWeb.Blazor.Services.Email;

/// <summary>
/// Sends the passwordless login email (magic link + one-time code). MoneyWeb only ever
/// sends this one kind of email, so this stays a single domain-specific method rather
/// than a generic message-composition abstraction.
/// </summary>
public interface IEmailSender
{
    Task SendLoginEmailAsync(string toEmail, string magicLinkUrl, string code, CancellationToken ct = default);
}
