using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Services;

/// <summary>
/// Simulates month-by-month debt payoff for all three strategies given a group's debts and budget.
/// </summary>
public class DebtPayoffService
{
    public DebtPayoffResult Calculate(IEnumerable<Debt> debts, decimal monthlyBudget)
    {
        var activeDebts = debts.Where(d => d.IsActive && d.Balance > 0).ToList();

        return new DebtPayoffResult
        {
            Avalanche    = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Avalanche),
            Snowball     = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Snowball),
            Custom       = Simulate(activeDebts, monthlyBudget, PayoffStrategy.Custom),
            MinimumOnly  = Simulate(activeDebts, monthlyBudget, PayoffStrategy.MinimumOnly)
        };
    }

    private static StrategyResult Simulate(List<Debt> debts, decimal monthlyBudget, PayoffStrategy strategy)
    {
        if (debts.Count == 0)
            return new StrategyResult();

        // Working copies — track remaining balance per debt id
        var balances = debts.ToDictionary(d => d.Id, d => d.Balance);
        var paidOffDates = new Dictionary<int, DateOnly>();
        decimal totalInterest = 0m;
        int month = 0;
        const int maxMonths = 600; // 50-year safety cap

        // Determine priority order for the focus debt
        var ordered = strategy switch
        {
            PayoffStrategy.Avalanche => debts.OrderByDescending(d => d.InterestRate).ThenBy(d => d.Id).ToList(),
            PayoffStrategy.Snowball  => debts.OrderBy(d => d.Balance).ThenBy(d => d.Id).ToList(),
            PayoffStrategy.Custom    => debts.OrderBy(d => d.GroupSortOrder).ThenBy(d => d.Id).ToList(),
            _                        => debts.OrderByDescending(d => d.InterestRate).ThenBy(d => d.Id).ToList()
        };

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        while (balances.Values.Any(b => b > 0) && month < maxMonths)
        {
            month++;
            decimal budgetRemaining = monthlyBudget;

            // Accrue interest on all active balances
            foreach (var debt in debts)
            {
                if (balances[debt.Id] <= 0) continue;
                var monthlyRate = debt.InterestRate / 12m;
                var interest = Math.Round(balances[debt.Id] * monthlyRate, 2);
                balances[debt.Id] += interest;
                totalInterest += interest;
            }

            // Pay minimums first
            foreach (var debt in debts)
            {
                if (balances[debt.Id] <= 0) continue;
                var payment = Math.Min(debt.MinimumPayment, balances[debt.Id]);
                balances[debt.Id] -= payment;
                budgetRemaining -= payment;
                if (balances[debt.Id] <= 0)
                {
                    balances[debt.Id] = 0;
                    paidOffDates.TryAdd(debt.Id, today.AddMonths(month));
                }
            }

            // Apply surplus to the current focus debt (first non-zero in priority order)
            if (budgetRemaining > 0 && strategy != PayoffStrategy.MinimumOnly)
            {
                foreach (var debt in ordered)
                {
                    if (balances[debt.Id] <= 0) continue;
                    var extra = Math.Min(budgetRemaining, balances[debt.Id]);
                    balances[debt.Id] -= extra;
                    budgetRemaining -= extra;
                    if (balances[debt.Id] <= 0)
                    {
                        balances[debt.Id] = 0;
                        paidOffDates.TryAdd(debt.Id, today.AddMonths(month));
                    }
                    break; // Only one focus debt per month
                }
            }
        }

        // Any debts still not paid off get max date
        foreach (var debt in debts)
            paidOffDates.TryAdd(debt.Id, today.AddMonths(month));

        return new StrategyResult
        {
            TotalInterestPaid = Math.Round(totalInterest, 2),
            TotalMonths       = month,
            PayoffDate        = today.AddMonths(month),
            DebtPayoffDates   = paidOffDates,
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
}
