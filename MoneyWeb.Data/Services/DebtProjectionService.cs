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

        await _cfRepo.DeleteProjectedForGroupAsync(groupId, userId, includeManualOverrides);

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

        await _cfRepo.BulkInsertProjectedAsync(transactions);
    }

    public async Task DeleteDebtProjectionsAsync(int debtId, int userId)
    {
        await _cfRepo.DeleteProjectedForSourceAsync(debtId, userId, TransactionCategory.DebtPayment);
    }

    private async Task PushUngroupedAsync(Debt debt, int userId)
    {
        if (!debt.BankAccountId.HasValue) return;

        await _cfRepo.DeleteProjectedForSourceAsync(debt.Id, userId, TransactionCategory.DebtPayment);

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

        await _cfRepo.BulkInsertProjectedAsync(transactions);
    }
}
