using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

public enum MonthItemKind { Bill, Debt, Income }

public enum MonthItemStatus
{
    Paid,           // bill confirmed / debt payment cleared
    Submitted,      // user sent the payment; awaiting clearance
    AssumedPaid,    // auto-draft whose date has passed — the bank pulled it without anything being recorded
    Upcoming,
    Overdue,
    Received,       // income
    Expected,       // income still to come
    Late            // income whose date has passed but hasn't been cleared in cash flow
}

public record MonthItem(MonthItemKind Kind, string Name, DateOnly Date, decimal Amount, MonthItemStatus Status, bool IsEstimate = false)
{
    public bool IsDone => Status is MonthItemStatus.Paid or MonthItemStatus.Submitted or MonthItemStatus.AssumedPaid or MonthItemStatus.Received;
}

public record AccountMonthBalance(string Name, decimal Now, decimal EndOfMonth);

public class MonthSnapshot
{
    public DateOnly MonthStart { get; init; }
    public DateOnly MonthEnd => MonthStart.AddMonths(1).AddDays(-1);

    /// <summary>Bills and debt payments due this month, done and still to go.</summary>
    public List<MonthItem> Outgoing { get; init; } = [];
    public List<MonthItem> Incoming { get; init; } = [];
    public List<AccountMonthBalance> Accounts { get; init; } = [];

    public IEnumerable<MonthItem> Paid => Outgoing.Where(i => i.IsDone);
    public IEnumerable<MonthItem> Left => Outgoing.Where(i => !i.IsDone);
    public IEnumerable<MonthItem> Received => Incoming.Where(i => i.IsDone);
    public IEnumerable<MonthItem> Expected => Incoming.Where(i => !i.IsDone);

    public decimal PaidTotal => Paid.Sum(i => i.Amount);
    public decimal LeftTotal => Left.Sum(i => i.Amount);
    public decimal ReceivedTotal => Received.Sum(i => i.Amount);
    public decimal ExpectedTotal => Expected.Sum(i => i.Amount);
    public decimal InTotal => ReceivedTotal + ExpectedTotal;
    public decimal OutTotal => PaidTotal + LeftTotal;
    public decimal Net => InTotal - OutTotal;
    public int OverdueCount => Outgoing.Count(i => i.Status == MonthItemStatus.Overdue);

    public decimal BalanceNow => Accounts.Sum(a => a.Now);
    public decimal BalanceEndOfMonth => Accounts.Sum(a => a.EndOfMonth);
}

public interface IMonthSnapshotService
{
    /// <summary>Where the user stands for the month containing <paramref name="today"/>: bills and debt payments
    /// paid vs. still due, income received vs. still expected, and bank balances now vs. projected month end.</summary>
    Task<MonthSnapshot> GetAsync(int userId, DateOnly today);
}

/// <summary>
/// Builds the dashboard's "this month" picture. Cleared cash-flow rows are deleted rather than kept, so what has
/// already happened is reconstructed from the records that do persist:
/// - Bills: each scheduled date this month is matched to a recorded occurrence (Confirmed/Submitted = paid,
///   otherwise still open). Unrecorded past auto-draft dates count as paid, as on the Bills page.
/// - Debts: LastPaymentDate in this month = paid; pending cash-flow payments are still due; otherwise the
///   payment day this month decides (auto-draft past = paid).
/// - Income: pay dates before NextPaymentDate were received (clearing advances it); the rest are expected.
/// - Balances: checking/savings current balance plus every pending cash-flow row through month end — the same
///   running balance the Transactions page shows.
/// </summary>
public class MonthSnapshotService(
    IBillRepository billRepo,
    IDebtRepository debtRepo,
    IIncomeRepository incomeRepo,
    IBankAccountRepository accountRepo,
    ICashFlowRepository cfRepo,
    BillProjectionService billProjection) : IMonthSnapshotService
{
    // Same lookback as the Transactions page, so the projected balance matches its running balance.
    private const int CashFlowYearsBack = 5;

    public async Task<MonthSnapshot> GetAsync(int userId, DateOnly today)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var accounts = (await accountRepo.GetAllAsync(userId)).ToList();
        var cashFlow = new Dictionary<int, List<CashFlowTransaction>>();
        foreach (var a in accounts)
            cashFlow[a.Id] = (await cfRepo.GetForAccountAsync(a.Id, userId, today.AddYears(-CashFlowYearsBack), monthEnd)).ToList();

        var outgoing = new List<MonthItem>();
        outgoing.AddRange(await BuildBillsAsync(userId, today, monthStart, monthEnd));
        outgoing.AddRange(await BuildDebtsAsync(userId, today, monthStart, monthEnd, cashFlow.Values.SelectMany(x => x)));

        return new MonthSnapshot
        {
            MonthStart = monthStart,
            Outgoing = outgoing.OrderBy(i => i.Date).ThenBy(i => i.Name).ToList(),
            Incoming = (await BuildIncomeAsync(userId, today, monthStart, monthEnd)).OrderBy(i => i.Date).ThenBy(i => i.Name).ToList(),
            Accounts = accounts
                .Where(a => a.AccountType is AccountType.Checking or AccountType.Savings)
                .Select(a => new AccountMonthBalance(a.Name, a.CurrentBalance, a.CurrentBalance + cashFlow[a.Id].Sum(t => t.Amount)))
                .ToList()
        };
    }

    private async Task<List<MonthItem>> BuildBillsAsync(int userId, DateOnly today, DateOnly monthStart, DateOnly monthEnd)
    {
        var items = new List<MonthItem>();
        var bills = (await billRepo.GetAllAsync(userId, activeOnly: true)).ToList();

        // Look a little past the month edges so a payment recorded a few days early/late still matches its date.
        var occurrences = (await billRepo.GetUpcomingOccurrencesAsync(userId, monthStart.AddDays(-15), monthEnd.AddDays(15))).ToList();
        var claimed = new HashSet<int>();

        foreach (var bill in bills)
        {
            var created = DateOnly.FromDateTime(bill.CreatedAt);
            var tolerance = bill.Frequency switch
            {
                BillFrequency.Weekly   => 3,
                BillFrequency.BiWeekly => 7,
                _                      => 15
            };

            foreach (var date in billProjection.GetOccurrenceDates(bill, monthStart, monthEnd))
            {
                var occ = occurrences
                    .Where(o => o.BillId == bill.Id && !claimed.Contains(o.Id)
                                && Math.Abs(o.DueDate.DayNumber - date.DayNumber) <= tolerance)
                    .OrderBy(o => Math.Abs(o.DueDate.DayNumber - date.DayNumber))
                    .FirstOrDefault();

                if (occ is not null)
                {
                    claimed.Add(occ.Id);
                    items.Add(FromOccurrence(occ, today));
                }
                else if (date >= created)   // dates before the bill existed were never tracked
                {
                    var status = date >= today ? MonthItemStatus.Upcoming
                        : bill.PaymentMethod == DebtPaymentMethod.AutoDraft ? MonthItemStatus.AssumedPaid
                        : MonthItemStatus.Overdue;
                    items.Add(new MonthItem(MonthItemKind.Bill, bill.Name, date, bill.Amount, status, IsEstimate: true));
                }
            }
        }

        // One-offs, payments for bills since deactivated, and any extra occurrences not matched to a schedule date.
        items.AddRange(occurrences
            .Where(o => !claimed.Contains(o.Id) && o.DueDate >= monthStart && o.DueDate <= monthEnd)
            .Select(o => FromOccurrence(o, today)));

        return items;
    }

    private static MonthItem FromOccurrence(BillOccurrence o, DateOnly today)
    {
        var date = o.PlannedPayDate ?? o.DueDate;
        var status = o.Status switch
        {
            BillOccurrenceStatus.Confirmed => MonthItemStatus.Paid,
            BillOccurrenceStatus.Submitted => MonthItemStatus.Submitted,
            _ => date < today ? MonthItemStatus.Overdue : MonthItemStatus.Upcoming
        };
        return new MonthItem(MonthItemKind.Bill, o.DisplayName, date, o.ActualAmount ?? o.EstimatedAmount, status, IsEstimate: o.ActualAmount is null);
    }

    private async Task<List<MonthItem>> BuildDebtsAsync(
        int userId, DateOnly today, DateOnly monthStart, DateOnly monthEnd, IEnumerable<CashFlowTransaction> cashFlow)
    {
        var items = new List<MonthItem>();
        var pendingByDebt = cashFlow
            .Where(t => t.Category == TransactionCategory.DebtPayment && t.ReferenceId.HasValue && t.TransactionDate <= monthEnd)
            .ToLookup(t => t.ReferenceId!.Value);

        foreach (var debt in (await debtRepo.GetAllAsync(userId, activeOnly: true)).Where(d => d.Balance > 0))
        {
            var paidThisMonth = debt.LastPaymentDate is { } last && last >= monthStart && last <= monthEnd;
            if (paidThisMonth)
                items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, debt.LastPaymentDate!.Value, debt.TotalMonthlyPayment, MonthItemStatus.Paid, IsEstimate: true));

            // Scheduled cash-flow payments still waiting to clear — includes any left over from earlier months.
            var pending = pendingByDebt[debt.Id].ToList();
            foreach (var t in pending)
            {
                var status = t.IsSubmitted ? MonthItemStatus.Submitted
                    : t.TransactionDate < today ? MonthItemStatus.Overdue
                    : MonthItemStatus.Upcoming;
                items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, t.TransactionDate, -t.Amount, status));
            }

            if (paidThisMonth || pending.Count > 0) continue;

            // Not in cash flow and not paid yet: fall back to the payment day on the debt itself.
            var due = debt.PaymentDayOfMonth is { } day
                ? new DateOnly(monthStart.Year, monthStart.Month, Math.Min(day, monthEnd.Day))
                : monthEnd;
            if (due < DateOnly.FromDateTime(debt.CreatedAt)) continue;
            var fallback = due >= today ? MonthItemStatus.Upcoming
                : debt.PaymentMethod == DebtPaymentMethod.AutoDraft ? MonthItemStatus.AssumedPaid
                : MonthItemStatus.Overdue;
            items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, due, debt.TotalMonthlyPayment, fallback, IsEstimate: true));
        }

        return items;
    }

    private async Task<List<MonthItem>> BuildIncomeAsync(int userId, DateOnly today, DateOnly monthStart, DateOnly monthEnd)
    {
        var items = new List<MonthItem>();
        foreach (var income in await incomeRepo.GetAllAsync(userId, activeOnly: true))
        {
            var created = DateOnly.FromDateTime(income.CreatedAt);

            // Clearing (or skipping) a paycheck advances NextPaymentDate, so pay dates before it were handled.
            for (var d = Step(income.Frequency, income.NextPaymentDate, back: true); d >= monthStart && d >= created; d = Step(income.Frequency, d, back: true))
            {
                if (d <= monthEnd)
                    items.Add(new MonthItem(MonthItemKind.Income, income.Name, d, income.EstimatedAmount, MonthItemStatus.Received, IsEstimate: true));
            }

            for (var d = income.NextPaymentDate; d <= monthEnd; d = Step(income.Frequency, d, back: false))
            {
                if (d >= monthStart)
                    items.Add(new MonthItem(MonthItemKind.Income, income.Name, d, income.EstimatedAmount,
                        d < today ? MonthItemStatus.Late : MonthItemStatus.Expected, IsEstimate: income.IsVariable));
            }
        }
        return items;
    }

    /// <summary>One pay period forward or back — mirrors <see cref="Income.AdvancePaymentDate"/>.</summary>
    private static DateOnly Step(IncomeFrequency frequency, DateOnly d, bool back) => frequency switch
    {
        IncomeFrequency.Weekly      => d.AddDays(back ? -7 : 7),
        IncomeFrequency.BiWeekly    => d.AddDays(back ? -14 : 14),
        IncomeFrequency.SemiMonthly => back
            ? (d.Day > 15 ? new DateOnly(d.Year, d.Month, 15)
               : d.Day > 1 ? new DateOnly(d.Year, d.Month, 1)
               : new DateOnly(d.AddMonths(-1).Year, d.AddMonths(-1).Month, 15))
            : (d.Day < 15 ? new DateOnly(d.Year, d.Month, 15)
               : new DateOnly(d.AddMonths(1).Year, d.AddMonths(1).Month, 1)),
        _                           => d.AddMonths(back ? -1 : 1)
    };
}
