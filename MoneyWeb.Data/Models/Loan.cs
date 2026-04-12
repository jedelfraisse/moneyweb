namespace MoneyWeb.Data.Models;

/// <summary>Money lent out to friends or family.</summary>
public class Loan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Borrower { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public decimal Principal { get; set; }
    public decimal InterestRate { get; set; }   // stored as fraction; 0 for interest-free
    public decimal AmountRepaid { get; set; }
    public decimal Balance => Principal - AmountRepaid;
    public DateOnly LoanDate { get; set; }
    public DateOnly? ExpectedRepaymentDate { get; set; }
    public bool IsSettled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Populated when retrieved via GetSharedWithMeAsync — the lender's display name.</summary>
    public string? OwnerDisplayName { get; set; }
}

public enum LoanTransactionType { Payment, Additional, Interest, Fee }

public class LoanTransaction
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int UserId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public LoanTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
