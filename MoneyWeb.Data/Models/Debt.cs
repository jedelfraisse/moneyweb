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

    /// <summary>Number of remaining monthly payment cycles that still fall at the promo rate, as of <paramref name="asOf"/>.</summary>
    public int PromoMonthsRemaining(DateOnly asOf)
    {
        if (!HasActivePromo(asOf)) return 0;
        int months = 0;
        while (asOf.AddMonths(months + 1) <= PromoExpirationDate!.Value) months++;
        return months;
    }

    /// <summary>
    /// Planning-only estimate: the flat monthly payment that would amortize this balance to exactly $0 by
    /// the promo's expiration date, at the promo rate (standard loan-amortization math; a plain Balance /
    /// months split when the rate is 0%). This is a projection, not a guarantee — it holds only if no new
    /// charges are added to the balance and every payment is made on schedule between now and expiration.
    /// Null unless a promo is currently active.
    /// </summary>
    public decimal? SuggestedPayoffByPromoExpiration(DateOnly asOf)
    {
        if (!HasActivePromo(asOf) || Balance <= 0) return null;
        var months = PromoMonthsRemaining(asOf);
        if (months <= 0) return Balance;
        var monthlyRate = PromoInterestRate!.Value / 12m;
        if (monthlyRate == 0m) return Math.Round(Balance / months, 2);
        // Standard amortization formula: payment = balance * rate / (1 - (1 + rate)^-months)
        var factor = 1m - (decimal)Math.Pow(1.0 + (double)monthlyRate, -months);
        return factor > 0 ? Math.Round(Balance * monthlyRate / factor, 2) : Math.Round(Balance / months, 2);
    }

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

    /// <summary>Monthly interest at the standard (go-to) rate, ignoring any active promo — what accrues once the promo ends.</summary>
    public decimal StandardMonthlyInterest => Math.Round(Balance * InterestRate / 12m, 2);

    /// <summary>
    /// True when the minimum payment wouldn't cover interest at the standard rate. Checked against the
    /// standard rate always — even while a promo is temporarily keeping the effective rate low — so a card
    /// cushioned by a 0% intro offer still gets flagged before the rate reverts and the balance starts growing.
    /// </summary>
    public bool IsMinBelowInterest => Balance > 0 && InterestRate > 0 && MinimumPayment < StandardMonthlyInterest;

    /// <summary>
    /// Planning-only estimate: the interest-only monthly cost if this card were maxed out to its CreditLimit,
    /// at the standard rate. Meant for credit cards/lines of credit sitting at a $0 balance, where there's
    /// nothing yet to warn about but it's still useful to see what a suggested minimum payment might look like
    /// if the limit were used. Not a stored value and never affects any calculation — purely informational.
    /// </summary>
    public decimal? SuggestedMinPaymentAtFullLimit =>
        CreditLimit.HasValue && CreditLimit.Value > 0 && InterestRate > 0
            ? Math.Round(CreditLimit.Value * InterestRate / 12m, 2)
            : null;

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
