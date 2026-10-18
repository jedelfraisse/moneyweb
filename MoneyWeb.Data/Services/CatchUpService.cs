using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

/// <summary>One thing Catch Up would change. <see cref="Actions"/> lists what the user can choose for it; the first is
/// the default. <see cref="Key"/> identifies it so a choice can be passed back to <see cref="ICatchUpService.ApplyAsync"/>.</summary>
public record CatchUpItem(string Key, string Area, string Name, DateOnly Date, decimal? Amount, IReadOnlyList<string> Actions, string? Detail = null)
{
    public string DefaultAction => Actions[0];
}

public record CatchUpPreview(DateOnly Cutoff, IReadOnlyList<CatchUpItem> Items)
{
    public int Count(string area) => Items.Count(i => i.Area == area);
    public bool IsEmpty => Items.Count == 0;
}

public interface ICatchUpService
{
    /// <summary>What Catch Up would change for everything dated before <paramref name="cutoff"/>.</summary>
    Task<CatchUpPreview> PreviewAsync(int userId, DateOnly cutoff);

    /// <summary>Treat everything dated before <paramref name="cutoff"/> as dealt with and restart projections from
    /// today. <paramref name="choices"/> maps an item's Key to one of its Actions; items not in it get their default.</summary>
    Task<CatchUpPreview> ApplyAsync(int userId, DateOnly cutoff, int horizonMonths, IReadOnlyDictionary<string, string>? choices = null);
}

/// <summary>
/// For when the app hasn't been kept up (e.g. not signed in for a while, or a stale test environment): rather than
/// clearing or skipping months of overdue items one by one, roll everything forward to a cutoff in one go, with a
/// per-item override. Bills closed this way are recorded as Confirmed with a note, so their schedules advance the
/// same way a real payment would; nothing on or after the cutoff is touched except to rebuild projections.
/// </summary>
public class CatchUpService(
    ICashFlowRepository cfRepo,
    IBillRepository billRepo,
    IIncomeRepository incomeRepo,
    ISinkingFundRepository fundRepo,
    IDebtRepository debtRepo,
    IDebtGroupRepository groupRepo,
    IDebtProjectionService debtProjection,
    ISinkingFundProjectionService fundProjection,
    IBillCashFlowSyncService billSync,
    BillProjectionService billProjection,
    IBankAccountRepository accountRepo) : ICatchUpService
{
    public const string Note = "Closed by Catch Up";

    public const string MarkPaid = "Mark paid";
    public const string KeepOpen = "Keep open";
    public const string Remove = "Remove";
    public const string Keep = "Keep";
    public const string MoveForward = "Move forward";
    public const string KeepAsIs = "Keep as is";

    public Task<CatchUpPreview> PreviewAsync(int userId, DateOnly cutoff) =>
        RunAsync(userId, cutoff, apply: false, horizonMonths: 0, choices: null);

    public Task<CatchUpPreview> ApplyAsync(int userId, DateOnly cutoff, int horizonMonths, IReadOnlyDictionary<string, string>? choices = null) =>
        RunAsync(userId, cutoff, apply: true, horizonMonths, choices);

    private async Task<CatchUpPreview> RunAsync(int userId, DateOnly cutoff, bool apply, int horizonMonths,
        IReadOnlyDictionary<string, string>? choices)
    {
        var items = new List<CatchUpItem>();
        string Choice(CatchUpItem i) =>
            choices is not null && choices.TryGetValue(i.Key, out var c) && i.Actions.Contains(c) ? c : i.DefaultAction;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var pending = (await cfRepo.GetPendingBeforeAsync(userId, cutoff)).ToList();
        var accountNames = (await accountRepo.GetAllAsync(userId)).ToDictionary(a => a.Id, a => a.Name);
        var autoDraftBillIds = (await billRepo.GetAllAsync(userId, activeOnly: false))
            .Where(b => b.PaymentMethod == DebtPaymentMethod.AutoDraft).Select(b => b.Id).ToHashSet();
        var handledRows = new HashSet<int>();   // cash-flow rows already decided by their bill

        // 1. Overdue open bills. Each one carries its own pending payment(s) along with it, so keeping a bill open
        //    keeps its payment, and marking it paid or removing it takes the payment out too.
        var openBills = (await billRepo.GetOpenOccurrencesAsync(userId))
            .Where(o => (o.PlannedPayDate ?? o.DueDate) < cutoff)
            .ToList();
        var closedBillDates = new List<(int BillId, DateOnly Due)>();
        foreach (var o in openBills)
        {
            var payDate = o.PlannedPayDate ?? o.DueDate;
            var rows = pending.Where(t => t.Category == TransactionCategory.Bill
                                          && (t.BillOccurrenceId == o.Id
                                              || (t.BillOccurrenceId is null && o.BillId.HasValue && t.ReferenceId == o.BillId && t.TransactionDate == payDate)))
                              .ToList();
            handledRows.UnionWith(rows.Select(r => r.Id));

            var detail = string.Join(" · ", new[]
            {
                o.BillId is { } bid0 && autoDraftBillIds.Contains(bid0) ? "Auto-draft" : null,
                rows.Count > 0 ? "with its cash-flow entry" : null
            }.Where(d => d is not null));
            var item = new CatchUpItem($"bill:{o.Id}", "Open bills", o.DisplayName, payDate, o.ActualAmount ?? o.EstimatedAmount,
                [MarkPaid, KeepOpen, Remove], detail.Length > 0 ? detail : null);
            items.Add(item);
            if (!apply) continue;

            var action = Choice(item);
            if (action == KeepOpen) continue;
            foreach (var r in rows) await cfRepo.DeleteAsync(r.Id, userId);
            if (action == MarkPaid)
            {
                o.Status = BillOccurrenceStatus.Confirmed;
                o.Notes = string.IsNullOrWhiteSpace(o.Notes) ? Note : $"{o.Notes} · {Note}";
                await billRepo.UpdateOccurrenceAsync(o);
                if (o.BillId is { } bid) closedBillDates.Add((bid, o.DueDate));
            }
            else
            {
                await billRepo.DeleteOccurrenceAsync(o.Id, userId);
            }
        }

        // 2. Recurring bills (paid by hand) whose next due date is still before the cutoff → record the last period
        //    before the cutoff as handled, so the next due date lands on/after it. Auto-drafts roll forward on their own.
        foreach (var bill in await billRepo.GetAllAsync(userId, activeOnly: true))
        {
            if (bill.PaymentMethod == DebtPaymentMethod.AutoDraft) continue;
            // In a preview nothing is closed yet, so assume the default (mark paid) for this bill's open occurrences.
            var closed = apply
                ? closedBillDates.Where(c => c.BillId == bill.Id).Select(c => (DateOnly?)c.Due).Max()
                : openBills.Where(o => o.BillId == bill.Id).Select(o => (DateOnly?)o.DueDate).Max();
            if (closed.HasValue && (bill.LastOccurrenceDueDate is null || closed > bill.LastOccurrenceDueDate))
                bill.LastOccurrenceDueDate = closed;

            var next = bill.NextDueDate(bill.NextFromAfterLastOccurrence(bill.LastOccurrenceDueDate, today));
            if (next >= cutoff) continue;
            var periods = billProjection.GetOccurrenceDates(bill, next, cutoff.AddDays(-1)).ToList();
            if (periods.Count == 0) continue;

            var item = new CatchUpItem($"schedule:{bill.Id}", "Bill schedules", bill.Name, next, bill.Amount, [MoveForward, KeepAsIs],
                $"{periods.Count} unrecorded period{(periods.Count == 1 ? "" : "s")} through {periods[^1]:MMM d} marked handled");
            items.Add(item);
            if (apply && Choice(item) == MoveForward)
            {
                await billRepo.CreateOccurrenceAsync(new BillOccurrence
                {
                    BillId = bill.Id, UserId = userId, DueDate = periods[^1],
                    EstimatedAmount = bill.Amount, Status = BillOccurrenceStatus.Confirmed, Notes = Note
                });
            }
        }

        // 3. Remaining pending cash flow before the cutoff → removed (the same as Skip on the Transactions page).
        foreach (var t in pending.Where(t => !handledRows.Contains(t.Id)))
        {
            var item = new CatchUpItem($"cf:{t.Id}", "Cash flow", t.Description, t.TransactionDate, t.Amount, [Remove, Keep],
                accountNames.TryGetValue(t.BankAccountId, out var acct) ? acct : "Account was deleted");
            items.Add(item);
            if (apply && Choice(item) == Remove)
                await cfRepo.DeleteAsync(t.Id, userId);
        }

        // 4. Income whose next pay date is before the cutoff → rolled forward to the first payday on/after it.
        var incomes = (await incomeRepo.GetAllAsync(userId, activeOnly: true)).ToList();
        foreach (var income in incomes.Where(i => i.NextPaymentDate < cutoff))
        {
            var from = income.NextPaymentDate;
            var probe = new Income { NextPaymentDate = from, Frequency = income.Frequency };
            while (probe.NextPaymentDate < cutoff) probe.NextPaymentDate = probe.AdvancePaymentDate();
            var item = new CatchUpItem($"income:{income.Id}", "Income", income.Name, from, income.EstimatedAmount, [MoveForward, KeepAsIs],
                $"Next payday → {probe.NextPaymentDate:MMM d}");
            items.Add(item);
            if (apply && Choice(item) == MoveForward)
            {
                income.NextPaymentDate = probe.NextPaymentDate;
                await incomeRepo.UpdateAsync(income);
            }
        }

        // 5. Sinking funds due before the cutoff → due date rolled forward by whole periods (saved amount kept).
        var funds = (await fundRepo.GetAllAsync(userId, activeOnly: true)).ToList();
        foreach (var fund in funds.Where(f => f.NextDueDate < cutoff))
        {
            var next = fund.NextDueDate;
            while (next < cutoff) next = next.AddMonths(fund.PeriodMonths);
            var item = new CatchUpItem($"fund:{fund.Id}", "Sinking funds", fund.Name, fund.NextDueDate, fund.TargetAmount, [MoveForward, KeepAsIs],
                $"Next due → {next:MMM d, yyyy}");
            items.Add(item);
            if (apply && Choice(item) == MoveForward)
            {
                fund.NextDueDate = next;
                await fundRepo.UpdateAsync(fund);
            }
        }

        // 6. Rebuild projections from today so upcoming items are complete again. Projections only replace upcoming
        //    generated rows, so anything the user chose to keep stays put.
        if (apply)
        {
            foreach (var income in incomes.Where(i => i.BankAccountId.HasValue))
                await RepushIncomeAsync(income, userId, horizonMonths);
            foreach (var fund in funds)
                await fundProjection.PushAsync(fund, userId, horizonMonths);
            foreach (var group in await groupRepo.GetAllAsync(userId))
                await debtProjection.PushGroupAsync(group.Id, userId);
            foreach (var debt in (await debtRepo.GetAllAsync(userId, activeOnly: true)).Where(d => d.GroupId is null))
                await debtProjection.PushDebtAsync(debt.Id, userId);
            await billSync.SyncAllAsync(userId);
        }

        return new CatchUpPreview(cutoff, items);
    }

    /// <summary>Same schedule the Income page pushes: one entry per payday from NextPaymentDate to the horizon. Only
    /// generated rows dated today or later are replaced, so an older payday the user kept stays.</summary>
    private async Task RepushIncomeAsync(Income income, int userId, int horizonMonths)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var t in (await cfRepo.GetForSourceAsync(income.Id, userId, TransactionCategory.Income))
                     .Where(t => t.IsProjected && !t.IsManualOverride && !t.IsSubmitted && t.TransactionDate >= today))
            await cfRepo.DeleteAsync(t.Id, userId);

        var end = DateOnly.FromDateTime(DateTime.Today.AddMonths(horizonMonths));
        var rows = new List<CashFlowTransaction>();
        var probe = new Income { NextPaymentDate = income.NextPaymentDate, Frequency = income.Frequency };
        while (probe.NextPaymentDate < today) probe.NextPaymentDate = probe.AdvancePaymentDate();
        while (probe.NextPaymentDate <= end)
        {
            rows.Add(new CashFlowTransaction
            {
                UserId = userId,
                BankAccountId = income.BankAccountId!.Value,
                TransactionDate = probe.NextPaymentDate,
                Description = $"Income: {income.Name}",
                Amount = income.EstimatedAmount,
                Category = TransactionCategory.Income,
                ReferenceId = income.Id,
                IsProjected = true
            });
            probe.NextPaymentDate = probe.AdvancePaymentDate();
        }
        if (rows.Count > 0) await cfRepo.BulkInsertProjectedAsync(rows);
    }
}
