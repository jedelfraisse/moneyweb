namespace MoneyWeb.Data.Models;

public enum SinkingFundFrequency
{
    Annual,
    SemiAnnual,
    Quarterly,
    Monthly
}

public class SinkingFund
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public SinkingFundFrequency Frequency { get; set; }
    public DateOnly NextDueDate { get; set; }
    public decimal? MonthlyContribution { get; set; }  // null = auto-calculate
    public decimal CurrentSaved { get; set; }
    public int? BankAccountId { get; set; }       // destination / savings bucket account
    public int? FundingAccountId { get; set; }    // source account contributions come FROM (optional)

    // Navigation — loaded by repository join
    public BankAccount? BankAccount { get; set; }
    public BankAccount? FundingAccount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Calculated properties ──────────────────────────────────────
    /// <summary>Months between payments based on Frequency.</summary>
    public int PeriodMonths => Frequency switch
    {
        SinkingFundFrequency.Annual     => 12,
        SinkingFundFrequency.SemiAnnual => 6,
        SinkingFundFrequency.Quarterly  => 3,
        SinkingFundFrequency.Monthly    => 1,
        _                               => 12
    };

    /// <summary>Whole months remaining until NextDueDate (minimum 1).</summary>
    public int MonthsRemaining
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (NextDueDate <= today) return 1;
            return Math.Max(1,
                ((NextDueDate.Year - today.Year) * 12) + NextDueDate.Month - today.Month);
        }
    }

    /// <summary>How much still needs to be saved.</summary>
    public decimal AmountNeeded => Math.Max(0, TargetAmount - CurrentSaved);

    /// <summary>Auto-calculated monthly contribution to hit target on time.</summary>
    public decimal AutoMonthlyContribution =>
        MonthsRemaining > 0 ? Math.Ceiling(AmountNeeded / MonthsRemaining * 100) / 100 : AmountNeeded;

    /// <summary>The effective monthly contribution (override if set, otherwise auto).</summary>
    public decimal EffectiveMonthlyContribution => MonthlyContribution ?? AutoMonthlyContribution;

    /// <summary>Percentage saved toward target (0–100).</summary>
    public int PercentSaved => TargetAmount > 0
        ? (int)Math.Min(100, CurrentSaved / TargetAmount * 100)
        : 0;

    /// <summary>True if saving at effective rate will cover the target in time.</summary>
    public bool IsOnTrack =>
        CurrentSaved >= TargetAmount ||
        EffectiveMonthlyContribution * MonthsRemaining >= AmountNeeded;
}
