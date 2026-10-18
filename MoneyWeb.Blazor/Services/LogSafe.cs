namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Makes user-supplied values safe to write to logs. Log files live on the host and are kept for a while, so they
/// shouldn't hold more customer information than needed (see the WISP), and user input must not be able to forge
/// extra log lines by embedding line breaks.
/// </summary>
public static class LogSafe
{
    /// <summary>Strips line breaks so the value can't start a fake log entry.</summary>
    public static string Text(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

    /// <summary>"jonathan@example.com" → "j***@example.com": enough to tell addresses apart while troubleshooting.</summary>
    public static string Email(string? email)
    {
        var clean = Text(email);
        var at = clean.IndexOf('@');
        return at <= 0 ? "***" : $"{clean[0]}***{clean[at..]}";
    }
}
