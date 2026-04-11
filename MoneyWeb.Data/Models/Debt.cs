namespace MoneyWeb.Data.Models;

public enum DebtPaymentMethod
{
    AutoDraft = 0,
    Manual = 1
}

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
    public bool IsFixedPayment { get; set; } = false; // when true, lender requires exact minimum — no extra payments allowed
    public int? PaymentDayOfMonth { get; set; }
    public DateOnly? LastPaymentDate { get; set; }
    public DebtPaymentMethod PaymentMethod { get; set; } = DebtPaymentMethod.Manual;
    public DateOnly? PayoffDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? OwnerDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation — fees loaded separately by repository
    public List<DebtFee> Fees { get; set; } = [];

    /// <summary>Sum of all active fees (taxes, insurance, etc.) not counted toward balance reduction.</summary>
    public decimal TotalMonthlyFees => Fees.Where(f => f.IsActive).Sum(f => f.Amount);

    /// <summary>Total monthly outflow: minimum payment + all active fees.</summary>
    public decimal TotalMonthlyPayment => MinimumPayment + TotalMonthlyFees;

    /// <summary>
    /// Next upcoming occurrence of PaymentDayOfMonth on or after today.
    /// Returns null when no payment day is configured.
    /// </summary>
    public DateOnly? NextPaymentDate
    {
        get
        {
            if (!PaymentDayOfMonth.HasValue) return null;
            var today = DateOnly.FromDateTime(DateTime.Today);
            int day = Math.Min(PaymentDayOfMonth.Value, DateTime.DaysInMonth(today.Year, today.Month));
            var candidate = new DateOnly(today.Year, today.Month, day);
            if (candidate < today)
            {
                var next = today.AddMonths(1);
                day = Math.Min(PaymentDayOfMonth.Value, DateTime.DaysInMonth(next.Year, next.Month));
                candidate = new DateOnly(next.Year, next.Month, day);
            }
            return candidate;
        }
    }
}
