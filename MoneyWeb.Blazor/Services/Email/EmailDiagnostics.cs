using System.Collections.Concurrent;

namespace MoneyWeb.Blazor.Services.Email;

public record EmailSendAttempt(
    string ToEmail, string Host, int Port, bool Success, string? Error, long ElapsedMs, DateTime AtUtc);

/// <summary>
/// Temporary debug aid: records the outcome of every login-email send attempt (which SMTP host/port
/// was actually dialed, whether it connected/authenticated/sent, how long it took, and any exception),
/// so the login page can show proof of connectivity instead of requiring a server-log tail. Singleton,
/// in-memory, capped — safe to delete once smtp4dev connectivity is confirmed working reliably.
/// </summary>
public class EmailDiagnostics
{
    private const int MaxItems = 20;
    private readonly ConcurrentQueue<EmailSendAttempt> _attempts = new();

    public void Record(EmailSendAttempt attempt)
    {
        _attempts.Enqueue(attempt);
        while (_attempts.Count > MaxItems && _attempts.TryDequeue(out _)) { }
    }

    /// <summary>Most recent attempt for a given recipient, or the most recent attempt overall if toEmail is null.</summary>
    public EmailSendAttempt? GetLatest(string? toEmail = null) =>
        _attempts.Reverse().FirstOrDefault(a => toEmail is null || a.ToEmail.Equals(toEmail, StringComparison.OrdinalIgnoreCase));
}
