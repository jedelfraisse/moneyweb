namespace MoneyWeb.Data.Models;

public enum DebtPaymentMethod
{
    AutoDraft = 0,
    Manual = 1
}

public enum PromoExpirationBehavior
{
    /// <summary>Interest simply starts accruing at InterestRate from the expiration date forward. Most bank cards.</summary>
    RevertToStandardRate = 0,

    /// <summary>
    /// If any balance remains at expiration, interest is retroactively charged (as a one-time lump sum) on
    /// PromoOriginalBalance at InterestRate for the promo's duration — typical of store-card "no interest if
    /// paid in full within N months" offers.
    /// </summary>
    DeferredInterest = 1
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

    /// <summary>Promotional/introductory rate (stored as fraction, e.g. 0 = 0% APR). Null when no promo is set.</summary>
    public decimal? PromoInterestRate { get; set; }

    /// <summary>Last date the promo rate applies. Null when no promo is set.</summary>
    public DateOnly? PromoExpirationDate { get; set; }

    /// <summary>Date the promo rate started. Defaults to today when a promo is first entered if left blank.</summary>
    public DateOnly? PromoStartDate { get; set; }

    /// <summary>
    /// Balance the promo terms applied to, captured when the promo was entered — the base for a
    /// DeferredInterest lump-sum charge. Defaults to the current Balance if not specified.
    /// </summary>
    public decimal? PromoOriginalBalance { get; set; }

    public PromoExpirationBehavior PromoExpirationBehavior { get; set; } = PromoExpirationBehavior.RevertToStandardRate;

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

    /// <summary>True while a promo rate is configured and today falls on/before its expiration date.</summary>
    public bool HasActivePromo(DateOnly asOf) =>
        PromoInterestRate.HasValue && PromoExpirationDate.HasValue && asOf <= PromoExpirationDate.Value;

    /// <summary>The rate that actually applies as of <paramref name="asOf"/> — the promo rate while active, InterestRate otherwise.</summary>
    public decimal EffectiveInterestRate(DateOnly asOf) => HasActivePromo(asOf) ? PromoInterestRate!.Value : InterestRate;

    /// <summary>True when a promo is active and expires within the next <paramref name="horizonMonths"/> months of <paramref name="asOf"/>.</summary>
    public bool IsPromoUrgent(DateOnly asOf, int horizonMonths) =>
        HasActivePromo(asOf) && PromoExpirationDate!.Value <= asOf.AddMonths(horizonMonths);

    /// <summary>
    /// Estimated deferred-interest lump sum charged at promo expiration if the balance isn't cleared in time —
    /// only meaningful when PromoExpirationBehavior is DeferredInterest. Approximated as simple interest on
    /// PromoOriginalBalance (or current Balance if unset) at InterestRate for the promo's duration.
    /// </summary>
    public decimal EstimatedDeferredInterest
    {
        get
        {
            if (PromoExpirationBehavior != PromoExpirationBehavior.DeferredInterest
                || !PromoExpirationDate.HasValue || !PromoStartDate.HasValue)
                return 0m;
            var promoBalance = PromoOriginalBalance ?? Balance;
            var months = Math.Max(0, PromoExpirationDate.Value.DayNumber - PromoStartDate.Value.DayNumber) / 30.44m;
            return Math.Round(promoBalance * InterestRate * months / 12m, 2);
        }
    }

    /// <summary>Monthly interest accrued on the current balance, using today's effective rate (promo if active).</summary>
    public decimal MonthlyInterest => Math.Round(Balance * EffectiveInterestRate(DateOnly.FromDateTime(DateTime.Today)) / 12m, 2);

    /// <summary>True when the minimum payment doesn't cover monthly interest — the balance will grow indefinitely.</summary>
    public bool IsMinBelowInterest => Balance > 0 && EffectiveInterestRate(DateOnly.FromDateTime(DateTime.Today)) > 0 && MinimumPayment < MonthlyInterest;

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
