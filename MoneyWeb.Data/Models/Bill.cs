namespace MoneyWeb.Data.Models;

public class Bill
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public BillFrequency Frequency { get; set; }
    public int DayDue { get; set; }             // day-of-month (or day-of-year for Annual)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum BillFrequency
{
    Monthly,
    Quarterly,
    Annual
}
