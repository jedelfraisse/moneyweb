namespace MoneyWeb.Data.Models;

public class Debt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? GroupId { get; set; }
    public int GroupSortOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Lender { get; set; }
    public decimal Balance { get; set; }
    public decimal InterestRate { get; set; }   // stored as fraction, e.g. 0.2199 = 21.99%
    public decimal MinimumPayment { get; set; }
    public DateOnly? PayoffDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
