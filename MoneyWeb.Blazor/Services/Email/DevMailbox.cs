using System.Collections.Concurrent;

namespace MoneyWeb.Blazor.Services.Email;

public record DevSentEmail(string ToEmail, string MagicLinkUrl, string Code, DateTime SentAtUtc);

/// <summary>In-memory, capped record of emails "sent" by <see cref="DevEmailSender"/>. Dev-only.</summary>
public class DevMailbox
{
    private const int MaxItems = 20;
    private readonly ConcurrentQueue<DevSentEmail> _sent = new();

    public void Add(DevSentEmail email)
    {
        _sent.Enqueue(email);
        while (_sent.Count > MaxItems && _sent.TryDequeue(out _)) { }
    }

    public IReadOnlyList<DevSentEmail> GetRecent() => _sent.Reverse().ToList();
}
