namespace MoneyWeb.Data.Models;

public class DebtGroup
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? BankAccountId { get; set; }
    public PayoffStrategy Strategy { get; set; }
    public decimal MonthlyBudget { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Populated by GetWithDebtsAsync — not a DB column
    public BankAccount? BankAccount { get; set; }
    public List<Debt> Debts { get; set; } = [];
}

public enum PayoffStrategy
{
    Avalanche   = 0,  // highest interest rate first
    Snowball    = 1,  // smallest balance first
    Custom      = 2,  // user-defined sort order
    MinimumOnly = 3   // pay minimums only, no rollover
}
