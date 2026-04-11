namespace MoneyWeb.Data.Models;

public class CashFlowTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BankAccountId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }             // positive = inflow, negative = outflow
    public TransactionCategory Category { get; set; }
    public int? ReferenceId { get; set; }           // FK to Debt, Bill, Income, or Loan depending on Category
    public int? DebtGroupId { get; set; }           // which debt group generated this row (for bulk regen)
    public bool IsProjected { get; set; } = true;   // false = confirmed/cleared
    public bool IsSubmitted { get; set; }           // user notified vendor; awaiting clearance
    public bool IsManualOverride { get; set; }      // user customised this date — skip on auto-regen
    public bool IsAutoDraft { get; set; }           // vendor pulls automatically; date is fixed
    public PayoffStrategy? GeneratedByStrategy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum TransactionCategory
{
    DebtPayment,
    Bill,
    LoanRepaymentReceived,
    Income,
    SinkingFundContribution,
    SinkingFundPayment,
    Other
}

/// <summary>
/// A single monthly debt payment from the simulation — used to populate projected cash flow.
/// </summary>
public record ScheduledPayment(
    int DebtId,
    string DebtName,
    DateOnly Date,
    decimal Amount,
    decimal FeesAmount,
    decimal BalanceAfter);
