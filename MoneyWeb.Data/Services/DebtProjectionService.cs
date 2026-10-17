using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

public class DebtProjectionService : IDebtProjectionService
{
    private const int HorizonMonths = 36;

    private readonly IDebtRepository _debtRepo;
    private readonly IDebtGroupRepository _groupRepo;
    private readonly ICashFlowRepository _cfRepo;
    private readonly DebtPayoffService _payoffSvc;

    public DebtProjectionService(
        IDebtRepository debtRepo,
        IDebtGroupRepository groupRepo,
        ICashFlowRepository cfRepo,
        DebtPayoffService payoffSvc)
    {
        _debtRepo = debtRepo;
        _groupRepo = groupRepo;
        _cfRepo = cfRepo;
        _payoffSvc = payoffSvc;
    }

    public async Task PushDebtAsync(int debtId, int userId)
    {
        var debt = await _debtRepo.GetByIdAsync(debtId, userId);
        if (debt is null) return;

        if (debt.GroupId.HasValue)
            await PushGroupAsync(debt.GroupId.Value, userId);
        else
            await PushUngroupedAsync(debt, userId);
    }

    public async Task PushGroupAsync(int groupId, int userId, bool includeManualOverrides = false)
    {
        var group = await _groupRepo.GetWithDebtsAsync(groupId, userId);
        if (group is null) return;

        var effectiveBanks = group.Debts.ToDictionary(
            d => d.Id,
            d => d.BankAccountId ?? group.BankAccountId);

        if (!effectiveBanks.Values.Any(id => id.HasValue)) return;

        // Per debt rather than per group, so rows a debt got before joining this group (ungrouped or another
        // group) are replaced too instead of lingering beside the new schedule as duplicates.
        foreach (var d in group.Debts)
            await DeleteUpcomingGeneratedAsync(d.Id, userId, includeManualOverrides);

        var result = _payoffSvc.Calculate(group.Debts, group.MonthlyBudget);
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddMonths(HorizonMonths));
        var schedule = result.ForStrategy(group.Strategy).PaymentSchedule
            .Where(p => p.Date <= cutoff);

        var autoDraftDebts = group.Debts.ToDictionary(
            d => d.Id,
            d => d.PaymentMethod == DebtPaymentMethod.AutoDraft);

        var transactions = schedule
            .Where(p => effectiveBanks.TryGetValue(p.DebtId, out var bid) && bid.HasValue)
            .Select(p => new CashFlowTransaction
            {
                UserId = userId,
                BankAccountId = effectiveBanks[p.DebtId]!.Value,
                TransactionDate = p.Date,
                Description = p.FeesAmount > 0
                    ? $"Debt Payment: {p.DebtName} (+ {p.FeesAmount.ToString("C")} fees)"
                    : $"Debt Payment: {p.DebtName}",
                Amount = -(p.Amount + p.FeesAmount),
                Category = TransactionCategory.DebtPayment,
                ReferenceId = p.DebtId,
                DebtGroupId = groupId,
                IsProjected = true,
                IsManualOverride = false,
                IsAutoDraft = autoDraftDebts.TryGetValue(p.DebtId, out var isAuto) && isAuto,
                GeneratedByStrategy = group.Strategy
            });

        await _cfRepo.BulkInsertProjectedAsync(await SkipPeriodsUserHandledAsync(transactions, userId));
    }

    private async Task PushUngroupedAsync(Debt debt, int userId)
    {
        // Clear first: a debt that just left a group may have no bank account now, and its old rows must still go.
        await DeleteUpcomingGeneratedAsync(debt.Id, userId, includeManualOverrides: false);
        if (!debt.BankAccountId.HasValue) return;

        var result = _payoffSvc.Calculate([debt], debt.MinimumPayment);
        var minOnly = result.ForStrategy(PayoffStrategy.MinimumOnly);
        if (minOnly.NeverPaysOff) return;

        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddMonths(HorizonMonths));
        var transactions = minOnly.PaymentSchedule
            .Where(p => p.Date <= cutoff)
            .Select(p => new CashFlowTransaction
            {
                UserId = userId,
                BankAccountId = debt.BankAccountId!.Value,
                TransactionDate = p.Date,
                Description = p.FeesAmount > 0
                    ? $"Debt Payment: {p.DebtName} (+ {p.FeesAmount.ToString("C")} fees)"
                    : $"Debt Payment: {p.DebtName}",
                Amount = -(p.Amount + p.FeesAmount),
                Category = TransactionCategory.DebtPayment,
                ReferenceId = p.DebtId,
                DebtGroupId = null,
                IsProjected = true,
                IsManualOverride = false,
                IsAutoDraft = debt.PaymentMethod == DebtPaymentMethod.AutoDraft,
                GeneratedByStrategy = PayoffStrategy.MinimumOnly
            });

        await _cfRepo.BulkInsertProjectedAsync(await SkipPeriodsUserHandledAsync(transactions, userId));
    }

    /// <summary>
    /// Remove a debt's generated payments dated today or later. Past-dated pending payments stay: they were never
    /// cleared or skipped, so they're still outstanding and keep counting against the balance until the user acts
    /// on them (as on the Transactions page and for bills). The new schedule only starts from today.
    /// </summary>
    private async Task DeleteUpcomingGeneratedAsync(int debtId, int userId, bool includeManualOverrides)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = (await _cfRepo.GetForSourceAsync(debtId, userId, TransactionCategory.DebtPayment))
            .Where(t => t.IsProjected && t.TransactionDate >= today
                        && (includeManualOverrides || (!t.IsManualOverride && !t.IsSubmitted)));
        foreach (var t in rows)
            await _cfRepo.DeleteAsync(t.Id, userId);
    }

    /// <summary>
    /// Rows the user has moved, edited or submitted survive regeneration on purpose — but each one still stands for
    /// a scheduled payment. Drop the generated payment nearest each such row (within half a month) so the period
    /// isn't paid twice in cash flow.
    /// </summary>
    private async Task<List<CashFlowTransaction>> SkipPeriodsUserHandledAsync(IEnumerable<CashFlowTransaction> generated, int userId)
    {
        var result = generated.ToList();
        foreach (var debtId in result.Select(t => t.ReferenceId!.Value).Distinct().ToList())
        {
            var kept = (await _cfRepo.GetForSourceAsync(debtId, userId, TransactionCategory.DebtPayment))
                .Where(t => t.IsProjected && (t.IsManualOverride || t.IsSubmitted));
            foreach (var k in kept)
            {
                var nearest = result
                    .Where(t => t.ReferenceId == debtId && Math.Abs(t.TransactionDate.DayNumber - k.TransactionDate.DayNumber) <= 15)
                    .OrderBy(t => Math.Abs(t.TransactionDate.DayNumber - k.TransactionDate.DayNumber))
                    .FirstOrDefault();
                if (nearest is not null) result.Remove(nearest);
            }
        }
        return result;
    }
}
