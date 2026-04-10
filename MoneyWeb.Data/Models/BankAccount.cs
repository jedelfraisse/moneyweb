namespace MoneyWeb.Data.Models;

public class BankAccount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum AccountType
{
    Checking,
    Savings,
    Credit,
    Investment
}
