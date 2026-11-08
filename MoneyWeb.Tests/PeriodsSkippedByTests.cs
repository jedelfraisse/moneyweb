using MoneyWeb.Data.Models;
using MoneyWeb.Data.Services;

namespace MoneyWeb.Tests;

public class PeriodsSkippedByTests
{
    private static readonly DateOnly Today = new(2026, 11, 7);
    private readonly BillProjectionService _projection = new();

    private static Bill MonthlyOn1st(DateOnly? lastRecorded) => new()
    {
        Frequency = BillFrequency.Monthly,
        DayDue = 1,
        PaymentMethod = DebtPaymentMethod.Manual,
        LastOccurrenceDueDate = lastRecorded
    };

    [Fact]
    public void Choosing_next_month_skips_the_unrecorded_period_before_it()
    {
        // Last recorded Oct 15, so the app expects Nov 1; the user says the next one is Dec 1.
        var bill = MonthlyOn1st(new DateOnly(2026, 10, 15));

        Assert.Equal([new DateOnly(2026, 11, 1)], _projection.PeriodsSkippedBy(bill, new DateOnly(2026, 12, 1), Today));
    }

    [Fact]
    public void Recording_the_last_skipped_period_moves_the_next_due_date_to_the_chosen_one()
    {
        var bill = MonthlyOn1st(new DateOnly(2026, 10, 15));
        var skipped = _projection.PeriodsSkippedBy(bill, new DateOnly(2026, 12, 1), Today);

        bill.LastOccurrenceDueDate = skipped[^1];

        Assert.Equal(new DateOnly(2026, 12, 1), bill.UpcomingDueDate(Today));
    }

    [Fact]
    public void Choosing_several_months_out_skips_every_period_in_between()
    {
        var bill = MonthlyOn1st(new DateOnly(2026, 10, 15));

        Assert.Equal([new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)],
            _projection.PeriodsSkippedBy(bill, new DateOnly(2027, 1, 1), Today));
    }

    [Fact]
    public void Keeping_the_upcoming_date_skips_nothing()
    {
        var bill = MonthlyOn1st(new DateOnly(2026, 10, 15));

        Assert.Empty(_projection.PeriodsSkippedBy(bill, new DateOnly(2026, 11, 1), Today));
    }

    [Fact]
    public void Choosing_an_earlier_date_skips_nothing()
    {
        var bill = MonthlyOn1st(new DateOnly(2026, 11, 1)); // upcoming Dec 1

        Assert.Empty(_projection.PeriodsSkippedBy(bill, new DateOnly(2026, 11, 1), Today));
    }

    [Fact]
    public void New_bill_starting_after_this_month_skips_this_months_date()
    {
        var bill = MonthlyOn1st(null); // nothing recorded → upcoming is the next 1st from today (Dec 1)

        Assert.Equal([new DateOnly(2026, 12, 1)], _projection.PeriodsSkippedBy(bill, new DateOnly(2027, 1, 1), Today));
    }

    [Fact]
    public void Annual_bill_moved_a_year_out_skips_this_years_date()
    {
        var bill = new Bill
        {
            Frequency = BillFrequency.Annual, DayDue = 30, AnnualMonth = 7,
            LastOccurrenceDueDate = new DateOnly(2025, 7, 30)
        };

        Assert.Equal([new DateOnly(2026, 7, 30)], _projection.PeriodsSkippedBy(bill, new DateOnly(2027, 7, 30), Today));
    }
}
