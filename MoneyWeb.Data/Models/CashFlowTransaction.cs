namespace MoneyWeb.Data.Models;

public class CashFlowTransaction
{
    public int Id { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }         // positive = inflow, negative = outflow
    public TransactionCategory Category { get; set; }
    public int? ReferenceId { get; set; }       // FK to Debt, Bill, or Loan depending on Category
    public DateTime CreatedAt { get; set; }
}

public enum TransactionCategory
{
    DebtPayment,
    Bill,
    LoanRepaymentReceived,
    Income,
    Other
}
