namespace MoneyWeb.Data.Models;

public class SharingContact
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public int? LinkedUserId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>True if the contact has logged in and linked their account.</summary>
    public bool IsLinked => LinkedUserId.HasValue;

    /// <summary>Best display label: override name, or email.</summary>
    public string Label => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : InvitedEmail;
}
