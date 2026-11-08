using MoneyWeb.Data.Models;

namespace MoneyWeb.Tests;

public class BillNextDueTests
{
    private static readonly DateOnly Today = new(2026, 11, 7);

    private static Bill Monthly16(DateOnly? lastRecorded) => new()
    {
        Frequency = BillFrequency.Monthly,
        DayDue = 16,
        PaymentMethod = DebtPaymentMethod.Manual,
        LastOccurrenceDueDate = lastRecorded
    };

    [Theory]
    [InlineData(16)] // on the scheduled day
    [InlineData(15)] // one day early
    [InlineData(13)] // a few days early — previously left November still owed
    [InlineData(5)]
    [InlineData(1)]  // half a month early is still this period
    public void Recording_this_periods_bill_early_moves_next_due_to_next_month(int recordedDay)
    {
        var bill = Monthly16(new DateOnly(2026, 11, recordedDay));

        Assert.Equal(new DateOnly(2026, 12, 16), bill.UpcomingDueDate(Today));
    }

    [Fact]
    public void Recording_a_bill_late_does_not_skip_the_next_period()
    {
        var bill = Monthly16(new DateOnly(2026, 10, 18)); // October's bill, two days late

        Assert.Equal(new DateOnly(2026, 11, 16), bill.UpcomingDueDate(Today));
    }

    [Fact]
    public void Recording_more_than_half_a_period_early_counts_as_the_previous_period()
    {
        var bill = Monthly16(new DateOnly(2026, 10, 31)); // 16 days before Nov 16

        Assert.Equal(new DateOnly(2026, 11, 16), bill.UpcomingDueDate(Today));
    }

    [Fact]
    public void Nothing_recorded_uses_the_next_scheduled_date()
    {
        Assert.Equal(new DateOnly(2026, 11, 16), Monthly16(null).UpcomingDueDate(Today));
    }

    [Fact]
    public void Weekly_bill_recorded_two_days_early_skips_that_week()
    {
        var bill = new Bill
        {
            Frequency = BillFrequency.Weekly,
            DayDue = (int)DayOfWeek.Friday,
            LastOccurrenceDueDate = new DateOnly(2026, 11, 11) // Wed, for Fri Nov 13
        };

        Assert.Equal(new DateOnly(2026, 11, 20), bill.UpcomingDueDate(Today));
    }

    [Fact]
    public void Weekly_bill_recorded_four_days_early_does_not_skip_that_week()
    {
        var bill = new Bill
        {
            Frequency = BillFrequency.Weekly,
            DayDue = (int)DayOfWeek.Friday,
            LastOccurrenceDueDate = new DateOnly(2026, 11, 9) // Mon, four days before Fri Nov 13
        };

        Assert.Equal(new DateOnly(2026, 11, 13), bill.UpcomingDueDate(Today));
    }
}
