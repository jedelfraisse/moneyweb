namespace MoneyWeb.Data.Models;

public enum IncomeType
{
    Salary = 0,
    Hourly = 1,
    Other = 2
}

public enum IncomeFrequency
{
    Weekly = 0,
    BiWeekly = 1,
    SemiMonthly = 2,    // 1st and 15th
    Monthly = 3
}

public class Income
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IncomeType IncomeType { get; set; }
    public bool IsVariable { get; set; }
    public decimal EstimatedAmount { get; set; }
    public IncomeFrequency Frequency { get; set; }
    public DateOnly NextPaymentDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? OwnerDisplayName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation (populated by join queries)
    public BankAccount? BankAccount { get; set; }

    /// <summary>Advances NextPaymentDate by one period based on Frequency.</summary>
    public DateOnly AdvancePaymentDate()
    {
        return Frequency switch
        {
            IncomeFrequency.Weekly      => NextPaymentDate.AddDays(7),
            IncomeFrequency.BiWeekly    => NextPaymentDate.AddDays(14),
            IncomeFrequency.SemiMonthly => AdvanceSemiMonthly(),
            IncomeFrequency.Monthly     => NextPaymentDate.AddMonths(1),
            _                           => NextPaymentDate.AddMonths(1)
        };
    }

    private DateOnly AdvanceSemiMonthly()
    {
        // Toggles between the 1st and 15th of the month
        if (NextPaymentDate.Day < 15)
            return new DateOnly(NextPaymentDate.Year, NextPaymentDate.Month, 15);
        var next = NextPaymentDate.AddMonths(1);
        return new DateOnly(next.Year, next.Month, 1);
    }
}
