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

/// <param name="SourceId">The Bill, Debt or Income id (null for one-off bills).</param>
/// <param name="CashFlowId">The pending cash-flow entry this item corresponds to, if any.</param>
public record MonthItem(
    MonthItemKind Kind, string Name, DateOnly Date, decimal Amount, MonthItemStatus Status,
    bool IsEstimate = false, int? SourceId = null, int? CashFlowId = null)
{
    public bool IsDone => Status is MonthItemStatus.Paid or MonthItemStatus.Submitted or MonthItemStatus.AssumedPaid or MonthItemStatus.Received;
    public bool InCashFlow => CashFlowId.HasValue;

    /// <summary>Signed effect on the bank balance: income positive, bills/payments negative.</summary>
    public decimal SignedAmount => Kind == MonthItemKind.Income ? Amount : -Amount;
}

/// <summary>A pending cash-flow entry on a checking/savings account, for the reconciliation list.</summary>
public record CashFlowLine(string Account, DateOnly Date, string Description, decimal Amount);

public record BalanceDay(DateOnly Date, decimal Balance, IReadOnlyList<CashFlowTransaction> Transactions);

public record AccountDaily(int AccountId, string Name, decimal Now, IReadOnlyList<BalanceDay> Days)
{
    public decimal EndBalance => Days.Count > 0 ? Days[^1].Balance : Now;
}

public class MonthSnapshot
{
    public DateOnly Today { get; init; }
    public DateOnly MonthStart { get; init; }
    public DateOnly MonthEnd => MonthStart.AddMonths(1).AddDays(-1);

    /// <summary>Bills and debt payments due this month, done and still to go.</summary>
    public List<MonthItem> Outgoing { get; init; } = [];
    public List<MonthItem> Incoming { get; init; } = [];

    /// <summary>Checking/savings accounts with their day-by-day projected balance from today to month end.</summary>
    public List<AccountDaily> Accounts { get; init; } = [];

    /// <summary>Pending checking/savings cash-flow entries through month end that no still-to-go item accounts
    /// for: sinking funds, manual entries, items left over from earlier months, and entries for bills or
    /// income already marked paid/received that were never cleared or skipped.</summary>
    public List<CashFlowLine> OtherPending { get; init; } = [];

    /// <summary>Amounts of cash-flow entries on checking/savings accounts, by id. Items matched to an entry elsewhere
    /// (e.g. a bill paid from a credit card) are in cash flow but don't move these balances.</summary>
    public Dictionary<int, decimal> CashAccountEntries { get; init; } = [];

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

    // ── Reconciliation: how "balance now" becomes "balance at month end" ──
    public decimal BalanceNow => Accounts.Sum(a => a.Now);

    /// <summary>Still-to-come/go items that have no cash-flow entry on any account, so no balance includes them.</summary>
    public List<MonthItem> NotInCashFlow => Expected.Concat(Left).Where(i => !i.InCashFlow).OrderBy(i => i.Date).ToList();

    /// <summary>Still-to-come/go items scheduled on a checking/savings account, at their cash-flow amounts.</summary>
    public decimal ScheduledToComeTotal => Expected.Sum(CashAmount);
    public decimal ScheduledToGoTotal => -Left.Sum(CashAmount);
    public decimal OtherPendingTotal => OtherPending.Sum(l => l.Amount);

    /// <summary>Projected from cash flow only — matches the Transactions page running balance.</summary>
    public decimal CashFlowEndOfMonth => Accounts.Sum(a => a.EndBalance);

    /// <summary>Cash-flow projection plus the items that aren't in cash flow yet, assuming they're paid from these accounts.</summary>
    public decimal EstimatedEndOfMonth => CashFlowEndOfMonth + NotInCashFlow.Sum(i => i.SignedAmount);

    private decimal CashAmount(MonthItem i) => i.CashFlowId is { } id && CashAccountEntries.TryGetValue(id, out var a) ? a : 0m;
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
///   running balance the Transactions page shows. Each item is then matched to its pending cash-flow entry so
///   the dashboard can show what the projection is missing and what it carries besides this month's items.
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
        var allPending = cashFlow.Values.SelectMany(x => x).ToList();

        var outgoing = new List<MonthItem>();
        outgoing.AddRange(await BuildBillsAsync(userId, today, monthStart, monthEnd));
        outgoing.AddRange(await BuildDebtsAsync(userId, today, monthStart, monthEnd, allPending));
        var incoming = await BuildIncomeAsync(userId, today, monthStart, monthEnd);

        // Match items to pending entries. Still-to-go/come items first, so an entry goes to the payment it
        // actually represents before a paid item that merely happens to be near it.
        var claimed = new HashSet<int>(outgoing.Concat(incoming).Where(i => i.CashFlowId.HasValue).Select(i => i.CashFlowId!.Value));
        outgoing = outgoing.Select(i => i.IsDone ? i : Match(i, allPending, claimed)).ToList();
        incoming = incoming.Select(i => i.IsDone ? i : Match(i, allPending, claimed)).ToList();
        outgoing = outgoing.Select(i => i.IsDone ? Match(i, allPending, claimed) : i).ToList();
        incoming = incoming.Select(i => i.IsDone ? Match(i, allPending, claimed) : i).ToList();

        var cashAccounts = accounts.Where(a => a.AccountType is AccountType.Checking or AccountType.Savings).ToList();
        var cashEntries = cashAccounts.SelectMany(a => cashFlow[a.Id]).ToDictionary(t => t.Id, t => t.Amount);
        var doneClaims = outgoing.Concat(incoming).Where(i => i.IsDone && i.CashFlowId.HasValue).Select(i => i.CashFlowId!.Value).ToHashSet();

        return new MonthSnapshot
        {
            Today = today,
            MonthStart = monthStart,
            Outgoing = outgoing.OrderBy(i => i.Date).ThenBy(i => i.Name).ToList(),
            Incoming = incoming.OrderBy(i => i.Date).ThenBy(i => i.Name).ToList(),
            Accounts = cashAccounts
                .Select(a => new AccountDaily(a.Id, a.Name, a.CurrentBalance, DailyBalances.Build(a.CurrentBalance, cashFlow[a.Id], today, monthEnd)))
                .ToList(),
            CashAccountEntries = cashEntries,
            OtherPending = cashAccounts
                .SelectMany(a => cashFlow[a.Id]
                    .Where(t => !claimed.Contains(t.Id) || doneClaims.Contains(t.Id))
                    .Select(t => new CashFlowLine(a.Name, t.TransactionDate, t.Description, t.Amount)))
                .OrderBy(l => l.Date)
                .ToList()
        };
    }

    /// <summary>Attach the nearest unclaimed pending entry for the same source (or, for one-offs, the same amount).</summary>
    private static MonthItem Match(MonthItem item, List<CashFlowTransaction> pending, HashSet<int> claimed)
    {
        if (item.CashFlowId.HasValue) return item;

        var (category, window) = item.Kind switch
        {
            MonthItemKind.Income => (TransactionCategory.Income, 7),
            MonthItemKind.Debt   => (TransactionCategory.DebtPayment, 15),
            _                    => (TransactionCategory.Bill, item.SourceId.HasValue ? 15 : 7)
        };
        var match = pending
            .Where(t => !claimed.Contains(t.Id) && t.Category == category
                        && (item.SourceId.HasValue ? t.ReferenceId == item.SourceId : t.ReferenceId == null && t.Amount == item.SignedAmount)
                        && Math.Abs(t.TransactionDate.DayNumber - item.Date.DayNumber) <= window)
            .OrderBy(t => Math.Abs(t.TransactionDate.DayNumber - item.Date.DayNumber))
            .FirstOrDefault();
        if (match is null) return item;
        claimed.Add(match.Id);
        return item with { CashFlowId = match.Id };
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
                    items.Add(new MonthItem(MonthItemKind.Bill, bill.Name, date, bill.Amount, status, IsEstimate: true, SourceId: bill.Id));
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
        return new MonthItem(MonthItemKind.Bill, o.DisplayName, date, o.ActualAmount ?? o.EstimatedAmount, status,
            IsEstimate: o.ActualAmount is null, SourceId: o.BillId);
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
                items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, debt.LastPaymentDate!.Value, debt.TotalMonthlyPayment,
                    MonthItemStatus.Paid, IsEstimate: true, SourceId: debt.Id));

            // Scheduled cash-flow payments still waiting to clear — includes any left over from earlier months.
            var pending = pendingByDebt[debt.Id].ToList();
            foreach (var t in pending)
            {
                var status = t.IsSubmitted ? MonthItemStatus.Submitted
                    : t.TransactionDate < today ? MonthItemStatus.Overdue
                    : MonthItemStatus.Upcoming;
                items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, t.TransactionDate, -t.Amount, status, SourceId: debt.Id, CashFlowId: t.Id));
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
            items.Add(new MonthItem(MonthItemKind.Debt, debt.Name, due, debt.TotalMonthlyPayment, fallback, IsEstimate: true, SourceId: debt.Id));
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
                    items.Add(new MonthItem(MonthItemKind.Income, income.Name, d, income.EstimatedAmount, MonthItemStatus.Received,
                        IsEstimate: true, SourceId: income.Id));
            }

            for (var d = income.NextPaymentDate; d <= monthEnd; d = Step(income.Frequency, d, back: false))
            {
                if (d >= monthStart)
                    items.Add(new MonthItem(MonthItemKind.Income, income.Name, d, income.EstimatedAmount,
                        d < today ? MonthItemStatus.Late : MonthItemStatus.Expected, IsEstimate: income.IsVariable, SourceId: income.Id));
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

public static class DailyBalances
{
    /// <summary>
    /// End-of-day balance for each day in [from, to], starting from the account's current balance. Pending entries
    /// dated before <paramref name="from"/> are still outstanding, so — as on the Transactions page — they're applied
    /// to the opening balance and shown with the first day.
    /// </summary>
    public static List<BalanceDay> Build(decimal currentBalance, IEnumerable<CashFlowTransaction> pending, DateOnly from, DateOnly to)
    {
        var rows = pending.Where(t => t.TransactionDate <= to).OrderBy(t => t.TransactionDate).ThenBy(t => t.Id).ToList();
        var byDay = rows.ToLookup(t => t.TransactionDate < from ? from : t.TransactionDate);
        var days = new List<BalanceDay>();
        var running = currentBalance;
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var today = byDay[d].ToList();
            running += today.Sum(t => t.Amount);
            days.Add(new BalanceDay(d, running, today));
        }
        return days;
    }
}
