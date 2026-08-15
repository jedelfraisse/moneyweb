namespace MoneyWeb.Data.Models;

public class User
{
    public int Id { get; set; }
    public string? EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsDeclined { get; set; }
    public string DeclineReason { get; set; } = string.Empty;
    public string RequestReason { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int CashFlowHorizonMonths { get; set; } = 12;
    public bool ShowSinkingFunds { get; set; } = true;
    public bool ShowLoans { get; set; } = true;
    public bool ShowFamily { get; set; } = false;
    public string LastLoginClaimsJson { get; set; } = string.Empty;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
