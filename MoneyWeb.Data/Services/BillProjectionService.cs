using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Services;

public class BillProjectionService
{
    /// <summary>Returns all occurrence dates for <paramref name="bill"/> within [from, to] inclusive.</summary>
    public IEnumerable<DateOnly> GetOccurrenceDates(Bill bill, DateOnly from, DateOnly to)
    {
        var cursor = FirstOccurrenceOnOrAfter(bill, from);
        while (cursor <= to)
        {
            yield return cursor;
            cursor = NextAfter(bill, cursor);
        }
    }

    private static DateOnly FirstOccurrenceOnOrAfter(Bill bill, DateOnly from)
    {
        return bill.Frequency switch
        {
            BillFrequency.Weekly     => NextWeekdayOnOrAfter(from, (DayOfWeek)(bill.DayDue % 7)),
            BillFrequency.BiWeekly   => NextWeekdayOnOrAfter(from, (DayOfWeek)(bill.DayDue % 7)),
            BillFrequency.Monthly    => NextMonthlyOnOrAfter(from, bill.DayDue),
            BillFrequency.BiMonthly  => NextMonthlyOnOrAfter(from, bill.DayDue),
            BillFrequency.Quarterly  => NextMonthlyOnOrAfter(from, bill.DayDue),
            BillFrequency.SemiAnnual => NextMonthlyOnOrAfter(from, bill.DayDue),
            BillFrequency.Annual     => NextAnnualOnOrAfter(bill, from),
            _                        => NextMonthlyOnOrAfter(from, bill.DayDue)
        };
    }

    private static DateOnly NextAfter(Bill bill, DateOnly current)
    {
        return bill.Frequency switch
        {
            BillFrequency.Weekly     => current.AddDays(7),
            BillFrequency.BiWeekly   => current.AddDays(14),
            BillFrequency.Monthly    => MonthlyDate(current.AddMonths(1), bill.DayDue),
            BillFrequency.BiMonthly  => MonthlyDate(current.AddMonths(2), bill.DayDue),
            BillFrequency.Quarterly  => MonthlyDate(current.AddMonths(3), bill.DayDue),
            BillFrequency.SemiAnnual => MonthlyDate(current.AddMonths(6), bill.DayDue),
            BillFrequency.Annual     => AnnualDate(current.Year + 1, bill.AnnualMonth ?? 1, bill.DayDue),
            _                        => MonthlyDate(current.AddMonths(1), bill.DayDue)
        };
    }

    private static DateOnly NextMonthlyOnOrAfter(DateOnly from, int day)
    {
        var candidate = MonthlyDate(from, day);
        return candidate >= from ? candidate : MonthlyDate(from.AddMonths(1), day);
    }

    private static DateOnly NextAnnualOnOrAfter(Bill bill, DateOnly from)
    {
        var candidate = AnnualDate(from.Year, bill.AnnualMonth ?? 1, bill.DayDue);
        return candidate >= from ? candidate : AnnualDate(from.Year + 1, bill.AnnualMonth ?? 1, bill.DayDue);
    }

    private static DateOnly NextWeekdayOnOrAfter(DateOnly from, DayOfWeek target)
    {
        int diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(diff);
    }

    private static DateOnly MonthlyDate(DateOnly anchor, int day)
        => new(anchor.Year, anchor.Month, Math.Min(day, DateTime.DaysInMonth(anchor.Year, anchor.Month)));

    private static DateOnly AnnualDate(int year, int month, int day)
        => new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
