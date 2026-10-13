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

public enum DebtFeeFrequency
{
    Monthly = 0,
    Quarterly = 1,
    Yearly = 2
}

public class DebtFee
{
    public int Id { get; set; }
    public int DebtId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>The amount charged per occurrence (not a monthly figure once Frequency isn't Monthly — see MonthlyEquivalent).</summary>
    public decimal Amount { get; set; }
    public DebtFeeCategory Category { get; set; } = DebtFeeCategory.Other;
    public bool IsActive { get; set; } = true;
    public DebtFeeFrequency Frequency { get; set; } = DebtFeeFrequency.Monthly;
    /// <summary>Day of month (1–31) the fee is due. Used for all frequencies.</summary>
    public int? DueDay { get; set; }
    /// <summary>1–12. For Yearly, the month it's due. For Quarterly, the anchor month of the 3-month cycle
    /// (e.g. DueMonth=1 → charged in Jan/Apr/Jul/Oct). Unused for Monthly.</summary>
    public int? DueMonth { get; set; }
    /// <summary>Optional — the Sinking Fund the user is saving into to cover this fee (e.g. property tax, HOA).</summary>
    public int? SinkingFundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation — loaded by repository join
    public string? SinkingFundName { get; set; }

    /// <summary>Months between occurrences — 1/3/12 for Monthly/Quarterly/Yearly.</summary>
    public int PeriodMonths => Frequency switch
    {
        DebtFeeFrequency.Quarterly => 3,
        DebtFeeFrequency.Yearly    => 12,
        _                          => 1
    };

    /// <summary>The amortized average monthly cost — what budgeting/dashboard totals should use, as opposed
    /// to Amount (the full per-occurrence charge, which only actually hits cash flow in its due month).</summary>
    public decimal MonthlyEquivalent => Amount / PeriodMonths;

    /// <summary>Whether this fee is actually charged in the given calendar month (1–12; the recurrence is
    /// year-independent) — vs. just contributing to the amortized MonthlyEquivalent average. Drives real
    /// cash-flow scheduling (DebtPayoffService).</summary>
    public bool IsDueIn(int month) => Frequency switch
    {
        DebtFeeFrequency.Monthly   => true,
        DebtFeeFrequency.Quarterly => Mod(month - (DueMonth ?? 1), 3) == 0,
        DebtFeeFrequency.Yearly    => month == (DueMonth ?? 1),
        _                          => true
    };

    private static int Mod(int a, int n) => ((a % n) + n) % n;
}
