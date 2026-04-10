namespace MoneyWeb.Data.Models;

/// <summary>Money lent out to friends or family.</summary>
public class Loan
{
    public int Id { get; set; }
    public string Borrower { get; set; } = string.Empty;
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
}
