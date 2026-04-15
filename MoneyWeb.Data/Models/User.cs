namespace MoneyWeb.Data.Models;

public class User
{
    public int Id { get; set; }
    public string EntraObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsAdmin { get; set; }
    public int CashFlowHorizonMonths { get; set; } = 12;
    public bool ShowSinkingFunds { get; set; } = true;
    public bool ShowLoans { get; set; } = true;
    public bool ShowFamily { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}
