using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Services;

/// <summary>
/// Simulates month-by-month debt payoff for all three strategies given a group's debts and budget.
/// </summary>
public class DebtPayoffService
{
    /// <summary>How far ahead of a promo's expiration it starts being prioritized over normal strategy ordering.</summary>
    private const int PromoUrgencyHorizonMonths = 3;

    public DebtPayoffResult Calculate(IEnumerable<Debt> debts, decimal monthlyBudget)
    {
        var activeDebts = debts.Where(d => d.IsActive && d.Balance > 0).ToList();
        var anyNeverPaysOff = activeDebts.Any(d => d.IsMinBelowInterest);

        return new DebtPayoffResult
        {
            Avalanche    = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Avalanche),
            Snowball     = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Snowball),
            Custom       = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Custom),
            MinimumOnly  = anyNeverPaysOff
                ? new StrategyResult { NeverPaysOff = true }
                : Simulate(activeDebts, monthlyBudget, PayoffStrategy.MinimumOnly)
        };
    }

    private static StrategyResult Simulate(List<Debt> debts, decimal monthlyBudget, PayoffStrategy strategy)
    {
        if (debts.Count == 0)
            return new StrategyResult();

        // Working copies — track remaining balance per debt id
        var balances = debts.ToDictionary(d => d.Id, d => d.Balance);
        var paidOffDates = new Dictionary<int, DateOnly>();
        var interestByDebt = debts.ToDictionary(d => d.Id, _ => 0m);
        var schedule = new List<ScheduledPayment>();
        const int scheduleMonthCap = 60; // 5-year schedule for cash flow display
        decimal totalInterest = 0m;
        int month = 0;
        const int maxMonths = 600; // 50-year safety cap

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Determine priority order for the focus debt as of a given date — any debt with a promo expiring
        // within PromoUrgencyHorizonMonths jumps to the front (soonest expiration first) so extra payments
        // help clear it before the rate reverts / deferred interest hits; otherwise falls back to strategy order.
        List<Debt> GetPriorityOrder(DateOnly asOf)
        {
            var strategyOrder = strategy switch
            {
                PayoffStrategy.Avalanche => debts.OrderByDescending(d => d.EffectiveInterestRate(asOf)).ThenBy(d => d.Id).ToList(),
                PayoffStrategy.Snowball  => debts.OrderBy(d => balances[d.Id]).ThenBy(d => d.Id).ToList(),
                PayoffStrategy.Custom    => debts.OrderBy(d => d.GroupSortOrder).ThenBy(d => d.Id).ToList(),
                _                        => debts.OrderByDescending(d => d.EffectiveInterestRate(asOf)).ThenBy(d => d.Id).ToList()
            };
            var urgent = strategyOrder.Where(d => d.IsPromoUrgent(asOf, PromoUrgencyHorizonMonths))
                                       .OrderBy(d => d.PromoExpirationDate);
            var rest = strategyOrder.Where(d => !d.IsPromoUrgent(asOf, PromoUrgencyHorizonMonths));
            return urgent.Concat(rest).ToList();
        }

        // Track which debts have already had a DeferredInterest lump sum applied, so it only fires once
        var deferredInterestCharged = new HashSet<int>();

        // Compute next-cycle payment amounts before simulation starts
        var nextCycleOrder = GetPriorityOrder(today.AddMonths(1));
        var minSum = debts.Where(d => balances[d.Id] > 0).Sum(d => d.MinimumPayment);
        var surplus = Math.Max(0m, monthlyBudget - minSum);
        var focusDebt = strategy != PayoffStrategy.MinimumOnly
            ? nextCycleOrder.FirstOrDefault(d => balances[d.Id] > 0 && !d.IsFixedPayment)
            : null;
        var nextPaymentAmounts = debts.ToDictionary(d => d.Id, d =>
        {
            if (balances[d.Id] <= 0) return 0m;
            var min = Math.Min(d.MinimumPayment, balances[d.Id]);
            if (focusDebt != null && d.Id == focusDebt.Id)
                return min + Math.Min(surplus, balances[d.Id] - min);
            return min;
        });

        while (balances.Values.Any(b => b > 0) && month < maxMonths)
        {
            month++;
            var currentDate = today.AddMonths(month);
            decimal budgetRemaining = monthlyBudget;

            // Track per-debt interest and payments for this month's schedule entry
            var interestThisMonth = debts.ToDictionary(d => d.Id, _ => 0m);
            var paymentsThisMonth = debts.ToDictionary(d => d.Id, _ => 0m);

            // Accrue interest on all active balances, at each debt's effective rate for this month
            // (promo rate while active, standard rate otherwise)
            foreach (var debt in debts)
            {
                if (balances[debt.Id] <= 0) continue;
                var monthlyRate = debt.EffectiveInterestRate(currentDate) / 12m;
                var interest = Math.Round(balances[debt.Id] * monthlyRate, 2);
                balances[debt.Id] += interest;
                interestByDebt[debt.Id] += interest;
                totalInterest += interest;
                if (month <= scheduleMonthCap)
                    interestThisMonth[debt.Id] = interest;
            }

            // One-time DeferredInterest lump sum: fires the month a promo expires if the card is set to
            // retroactively charge interest and still carries a balance at that point.
            foreach (var debt in debts)
            {
                if (deferredInterestCharged.Contains(debt.Id) || balances[debt.Id] <= 0) continue;
                if (debt.PromoExpirationBehavior != PromoExpirationBehavior.DeferredInterest) continue;
                if (debt.PromoExpirationDate is not { } expiration || expiration > currentDate) continue;
                deferredInterestCharged.Add(debt.Id);
                var lumpSum = debt.EstimatedDeferredInterest;
                if (lumpSum <= 0) continue;
                balances[debt.Id] += lumpSum;
                interestByDebt[debt.Id] += lumpSum;
                totalInterest += lumpSum;
                if (month <= scheduleMonthCap)
                    interestThisMonth[debt.Id] += lumpSum;
            }

            // Pay minimums first — track payments per debt this month for the schedule
            foreach (var debt in debts)
            {
                if (balances[debt.Id] <= 0) continue;
                var payment = Math.Min(debt.MinimumPayment, balances[debt.Id]);
                balances[debt.Id] -= payment;
                budgetRemaining -= payment;
                paymentsThisMonth[debt.Id] += payment;
                if (balances[debt.Id] <= 0)
                {
                    balances[debt.Id] = 0;
                    paidOffDates.TryAdd(debt.Id, currentDate);
                }
            }

            // Apply surplus to the current focus debt (first non-zero flexible debt in priority order,
            // recomputed each month so an approaching promo expiration can pull a debt to the front)
            if (budgetRemaining > 0 && strategy != PayoffStrategy.MinimumOnly)
            {
                foreach (var debt in GetPriorityOrder(currentDate))
                {
                    if (balances[debt.Id] <= 0 || debt.IsFixedPayment) continue;
                    var extra = Math.Min(budgetRemaining, balances[debt.Id]);
                    balances[debt.Id] -= extra;
                    budgetRemaining -= extra;
                    paymentsThisMonth[debt.Id] += extra;
                    if (balances[debt.Id] <= 0)
                    {
                        balances[debt.Id] = 0;
                        paidOffDates.TryAdd(debt.Id, currentDate);
                    }
                    break; // Only one focus debt per month
                }
            }

            // Record scheduled payments (capped at scheduleMonthCap for UI)
            if (month <= scheduleMonthCap)
            {
                foreach (var debt in debts)
                {
                    var paid = paymentsThisMonth[debt.Id];
                    if (paid <= 0) continue;
                    int day = debt.PaymentDayOfMonth.HasValue
                        ? Math.Min(debt.PaymentDayOfMonth.Value, DateTime.DaysInMonth(currentDate.Year, currentDate.Month))
                        : 1;
                    var dueDate = new DateOnly(currentDate.Year, currentDate.Month, day);
                    var fees = debt.Fees.Where(f => f.IsActive).Sum(f => f.Amount);
                    schedule.Add(new ScheduledPayment(debt.Id, debt.Name, dueDate, paid, fees, balances[debt.Id], interestThisMonth.GetValueOrDefault(debt.Id)));
                }
            }

            // Any debt whose minimum payment can't cover interest will never reach zero.
            // The maxMonths cap (600 months / 50 years) handles termination in that case,
            // which correctly produces a large total-interest figure for MinimumOnly.
        }

        // Any debts still not paid off get max date
        foreach (var debt in debts)
            paidOffDates.TryAdd(debt.Id, today.AddMonths(month));

        return new StrategyResult
        {
            TotalInterestPaid  = Math.Round(totalInterest, 2),
            TotalMonths        = month,
            PayoffDate         = today.AddMonths(month),
            DebtPayoffDates    = paidOffDates,
            DebtInterestPaid   = interestByDebt.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 2)),
            NextPaymentAmounts = nextPaymentAmounts,
            PaymentSchedule    = schedule,
            BudgetShortfall   = debts.Where(d => balances.GetValueOrDefault(d.Id) > 0 && d.Balance > 0)
                                     .Sum(d => d.MinimumPayment) > monthlyBudget
                                ? debts.Sum(d => d.MinimumPayment) - monthlyBudget
                                : 0m
        };
    }
}

public class DebtPayoffResult
{
    public StrategyResult Avalanche   { get; init; } = new();
    public StrategyResult Snowball    { get; init; } = new();
    public StrategyResult Custom      { get; init; } = new();
    public StrategyResult MinimumOnly { get; init; } = new();

    public StrategyResult ForStrategy(PayoffStrategy s) => s switch
    {
        PayoffStrategy.Snowball    => Snowball,
        PayoffStrategy.Custom      => Custom,
        PayoffStrategy.MinimumOnly => MinimumOnly,
        _                          => Avalanche
    };
}

public class StrategyResult
{
    public decimal TotalInterestPaid { get; init; }
    public int TotalMonths           { get; init; }
    public DateOnly PayoffDate       { get; init; }
    public decimal BudgetShortfall   { get; init; }
    /// <summary>Maps Debt.Id → projected payoff date under this strategy.</summary>
    public Dictionary<int, DateOnly> DebtPayoffDates { get; init; } = [];
    /// <summary>Maps Debt.Id → total interest paid over the life of this debt under this strategy.</summary>
    public Dictionary<int, decimal> DebtInterestPaid { get; init; } = [];
    /// <summary>Maps Debt.Id → the payment amount for the upcoming payment cycle (min + any surplus for the focus debt).</summary>
    public Dictionary<int, decimal> NextPaymentAmounts { get; init; } = [];
    /// <summary>Month-by-month payment schedule (capped at 60 months) for generating projected cash flow transactions.</summary>
    public List<ScheduledPayment> PaymentSchedule { get; init; } = [];
    /// <summary>True when at least one debt's minimum payment does not cover its monthly interest — balance grows forever.</summary>
    public bool NeverPaysOff { get; init; }
}
