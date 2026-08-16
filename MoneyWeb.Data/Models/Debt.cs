namespace MoneyWeb.Data.Models;

public enum DebtPaymentMethod
{
    AutoDraft = 0,
    Manual = 1
}

public enum DebtType
{
    Other = 0,
    CreditCard = 1,
    Mortgage = 2,
    AutoLoan = 3,
    StudentLoan = 4,
    PersonalLoan = 5,
    LineOfCredit = 6
}

public class Debt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? GroupId { get; set; }
    public int GroupSortOrder { get; set; }
    public int? BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Lender { get; set; }
    public DebtType DebtType { get; set; } = DebtType.Other;
    public decimal Balance { get; set; }
    public decimal? CreditLimit { get; set; }   // only relevant for credit card and line of credit debt types
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
    public BankAccount? BankAccount { get; set; }

    /// <summary>Credit utilization ratio (Balance / CreditLimit). Null when CreditLimit is not set or zero.</summary>
    public decimal? CreditUsage => CreditLimit.HasValue && CreditLimit.Value > 0
        ? Math.Round(Balance / CreditLimit.Value, 4)
        : null;

    /// <summary>Sum of all active fees (taxes, insurance, etc.) not counted toward balance reduction.</summary>
    public decimal TotalMonthlyFees => Fees.Where(f => f.IsActive).Sum(f => f.Amount);

    /// <summary>Total monthly outflow: minimum payment + all active fees.</summary>
    public decimal TotalMonthlyPayment => MinimumPayment + TotalMonthlyFees;

    /// <summary>Monthly interest accrued on the current balance.</summary>
    public decimal MonthlyInterest => Math.Round(Balance * InterestRate / 12m, 2);

    /// <summary>True when the minimum payment doesn't cover monthly interest — the balance will grow indefinitely.</summary>
    public bool IsMinBelowInterest => Balance > 0 && InterestRate > 0 && MinimumPayment < MonthlyInterest;

    /// <summary>
    /// Next upcoming occurrence of PaymentDayOfMonth strictly after the later of today or LastPaymentDate.
    /// This prevents showing a date that has already been paid (including future-dated payments).
    /// Returns null when no payment day is configured.
    /// </summary>
    public DateOnly? NextPaymentDate
    {
        get
        {
            if (!PaymentDayOfMonth.HasValue) return null;
            var today = DateOnly.FromDateTime(DateTime.Today);
            // If the last payment was made on or after today, use that as the anchor so we skip past it
            var from = LastPaymentDate.HasValue && LastPaymentDate.Value >= today
                ? LastPaymentDate.Value
                : today.AddDays(-1);
            int day = Math.Min(PaymentDayOfMonth.Value, DateTime.DaysInMonth(from.Year, from.Month));
            var candidate = new DateOnly(from.Year, from.Month, day);
            if (candidate <= from)
            {
                var next = from.AddMonths(1);
                day = Math.Min(PaymentDayOfMonth.Value, DateTime.DaysInMonth(next.Year, next.Month));
                candidate = new DateOnly(next.Year, next.Month, day);
            }
            return candidate;
        }
    }
}
