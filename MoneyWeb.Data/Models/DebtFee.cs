namespace MoneyWeb.Data.Models;

public enum DebtFeeCategory
{
    PropertyTax = 0,
    HomeownersInsurance = 1,
    PMI = 2,
    HOA = 3,
    FloodInsurance = 4,
    Other = 5
}

public class DebtFee
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DebtFeeCategory Category { get; set; } = DebtFeeCategory.Other;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
