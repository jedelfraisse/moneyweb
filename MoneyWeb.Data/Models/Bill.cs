namespace MoneyWeb.Data.Models;

public class Bill
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }                     // estimated amount
    public BillFrequency Frequency { get; set; }
    public int DayDue { get; set; }                         // day-of-month (1–31)
    public int? AnnualMonth { get; set; }                   // 1–12; only used when Frequency = Annual
    public BillCategory Category { get; set; }
    public DebtPaymentMethod PaymentMethod { get; set; } = DebtPaymentMethod.Manual;
    public int? BankAccountId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigational — populated by join queries
    public string? BankAccountName { get; set; }
    public DateOnly? LastOccurrenceDueDate { get; set; }   // most recent recorded occurrence

    public DateOnly NextDueDate(DateOnly from)
    {
        return Frequency switch
        {
            BillFrequency.Weekly      => NextWeekday(from, DayDue),
            BillFrequency.BiWeekly    => NextWeekday(from, DayDue).AddDays(7),
            BillFrequency.Monthly     => NextMonthlyDate(from, DayDue),
            BillFrequency.BiMonthly   => NextMonthlyDate(from, DayDue),
            BillFrequency.Quarterly   => NextMonthlyDate(from, DayDue),
            BillFrequency.SemiAnnual  => NextMonthlyDate(from, DayDue),
            BillFrequency.Annual      => NextAnnualDate(from),
            _                         => NextMonthlyDate(from, DayDue)
        };
    }

    /// <summary>
    /// Returns the "from" date to use when projecting the NEXT occurrence after one has been recorded.
    /// If the canonical scheduled occurrence falls within 1 day of lastDueDate (meaning the user just
    /// handled that period's bill), we advance past it so projections don't repeat the same period.
    /// Example: monthly-on-15 bill, user enters April 14 → canonical = April 15 → returns April 16 → next = May 15.
    /// </summary>
    public DateOnly NextFromAfterOccurrence(DateOnly occurrenceDueDate)
    {
        var canonical = NextDueDate(occurrenceDueDate);
        return canonical <= occurrenceDueDate.AddDays(1)
            ? canonical.AddDays(1)
            : occurrenceDueDate.AddDays(1);
    }

    /// <summary>Convenience overload that handles the null (no occurrences recorded) case.</summary>
    public DateOnly NextFromAfterLastOccurrence(DateOnly? lastOccurrenceDueDate, DateOnly today)
        => lastOccurrenceDueDate.HasValue
            ? NextFromAfterOccurrence(lastOccurrenceDueDate.Value)
            : today;

    private static DateOnly NextMonthlyDate(DateOnly from, int day)
    {
        var candidate = new DateOnly(from.Year, from.Month, Math.Min(day, DateTime.DaysInMonth(from.Year, from.Month)));
        return candidate >= from ? candidate : candidate.AddMonths(1) is var next
            ? new DateOnly(next.Year, next.Month, Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month)))
            : candidate;
    }

    private static DateOnly NextWeekday(DateOnly from, int dayOfWeek)
    {
        var d = from;
        while ((int)d.DayOfWeek != dayOfWeek % 7)
            d = d.AddDays(1);
        return d;
    }

    private DateOnly NextAnnualDate(DateOnly from)
    {
        int month = AnnualMonth ?? 1;
        int maxDay = DateTime.DaysInMonth(from.Year, month);
        var candidate = new DateOnly(from.Year, month, Math.Min(DayDue, maxDay));
        if (candidate >= from) return candidate;
        maxDay = DateTime.DaysInMonth(from.Year + 1, month);
        return new DateOnly(from.Year + 1, month, Math.Min(DayDue, maxDay));
    }
}

public class BillOccurrence
{
    public int Id { get; set; }
    public int? BillId { get; set; }        // null for one-off occurrences
    public int UserId { get; set; }
    public string? Name { get; set; }       // used for one-offs when BillId is null
    public DateOnly DueDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public BillOccurrenceStatus Status { get; set; }
    public DateOnly? PlannedPayDate { get; set; }
    public DateOnly? SubmittedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Populated by join queries
    public string? BillName { get; set; }

    /// <summary>Display name — uses BillName from the join for recurring, or Name for one-offs.</summary>
    public string DisplayName => BillName ?? Name ?? "(unnamed)";
    public bool IsOneOff => BillId is null;
}

public enum BillFrequency
{
    Weekly,
    BiWeekly,
    Monthly,
    BiMonthly,
    Quarterly,
    SemiAnnual,
    Annual
}

public enum BillCategory
{
    Utilities,
    Insurance,
    Subscriptions,
    Phone,
    Internet,
    Housing,
    Transportation,
    Medical,
    Entertainment,
    Other
}

public enum BillOccurrenceStatus
{
    Estimated,
    Received,
    Submitted,
    Confirmed
}
