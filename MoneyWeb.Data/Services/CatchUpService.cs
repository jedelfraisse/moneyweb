using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

public record CatchUpItem(string Area, string Name, DateOnly Date, decimal? Amount, string Change);

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
    /// today: pending cash flow before the cutoff is skipped, overdue bills are closed as paid, stale bill / income /
    /// sinking-fund dates roll forward, and debt, bill, income and fund projections are rebuilt.</summary>
    Task<CatchUpPreview> ApplyAsync(int userId, DateOnly cutoff, int horizonMonths);
}

/// <summary>
/// For when the app hasn't been kept up (e.g. not signed in for a while, or a stale test environment): rather than
/// clearing or skipping months of overdue items one by one, roll everything forward to a cutoff in one go.
/// Bills closed this way are recorded as Confirmed with a note, so their schedules advance the same way a real
/// payment would; nothing on or after the cutoff is touched except to rebuild projections.
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
    BillProjectionService billProjection) : ICatchUpService
{
    public const string Note = "Closed by Catch Up";

    public Task<CatchUpPreview> PreviewAsync(int userId, DateOnly cutoff) => RunAsync(userId, cutoff, apply: false, horizonMonths: 0);

    public Task<CatchUpPreview> ApplyAsync(int userId, DateOnly cutoff, int horizonMonths) => RunAsync(userId, cutoff, apply: true, horizonMonths);

    private async Task<CatchUpPreview> RunAsync(int userId, DateOnly cutoff, bool apply, int horizonMonths)
    {
        var items = new List<CatchUpItem>();

        // 1. Overdue open bills → closed as paid. Done first: it moves each bill's "last occurrence" forward.
        var openBills = (await billRepo.GetOpenOccurrencesAsync(userId))
            .Where(o => (o.PlannedPayDate ?? o.DueDate) < cutoff)
            .ToList();
        foreach (var o in openBills)
        {
            items.Add(new CatchUpItem("Open bills", o.DisplayName, o.PlannedPayDate ?? o.DueDate,
                o.ActualAmount ?? o.EstimatedAmount, "Marked paid"));
            if (apply)
            {
                o.Status = BillOccurrenceStatus.Confirmed;
                o.Notes = string.IsNullOrWhiteSpace(o.Notes) ? Note : $"{o.Notes} · {Note}";
                await billRepo.UpdateOccurrenceAsync(o);
            }
        }

        // 2. Recurring bills (paid by hand) whose next due date is still before the cutoff → record the last
        //    period before the cutoff as handled, so the next due date lands on/after it. Auto-drafts already roll
        //    forward on their own.
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var bill in await billRepo.GetAllAsync(userId, activeOnly: true))
        {
            if (bill.PaymentMethod == DebtPaymentMethod.AutoDraft) continue;
            // Account for any occurrence just closed in step 1 (not yet re-read from the database).
            var lastClosed = openBills.Where(o => o.BillId == bill.Id).Select(o => (DateOnly?)o.DueDate).Max();
            if (lastClosed.HasValue && (bill.LastOccurrenceDueDate is null || lastClosed > bill.LastOccurrenceDueDate))
                bill.LastOccurrenceDueDate = lastClosed;

            var next = bill.NextDueDate(bill.NextFromAfterLastOccurrence(bill.LastOccurrenceDueDate, today));
            if (next >= cutoff) continue;
            var lastBeforeCutoff = billProjection.GetOccurrenceDates(bill, next, cutoff.AddDays(-1)).LastOrDefault();
            if (lastBeforeCutoff == default) continue;

            var skipped = billProjection.GetOccurrenceDates(bill, next, cutoff.AddDays(-1)).Count();
            items.Add(new CatchUpItem("Bill schedules", bill.Name, next, bill.Amount,
                $"{skipped} unrecorded period{(skipped == 1 ? "" : "s")} through {lastBeforeCutoff:MMM d} marked handled"));
            if (apply)
            {
                await billRepo.CreateOccurrenceAsync(new BillOccurrence
                {
                    BillId = bill.Id, UserId = userId, DueDate = lastBeforeCutoff,
                    EstimatedAmount = bill.Amount, Status = BillOccurrenceStatus.Confirmed, Notes = Note
                });
            }
        }

        // 3. Pending cash flow before the cutoff → removed (the same as Skip on the Transactions page).
        foreach (var t in await cfRepo.GetPendingBeforeAsync(userId, cutoff))
            items.Add(new CatchUpItem("Cash flow", t.Description, t.TransactionDate, t.Amount, "Removed"));
        if (apply)
            await cfRepo.DeletePendingBeforeAsync(userId, cutoff);

        // 4. Income whose next pay date is before the cutoff → rolled forward to the first payday on/after it.
        var incomes = (await incomeRepo.GetAllAsync(userId, activeOnly: true)).ToList();
        foreach (var income in incomes.Where(i => i.NextPaymentDate < cutoff))
        {
            var from = income.NextPaymentDate;
            while (income.NextPaymentDate < cutoff) income.NextPaymentDate = income.AdvancePaymentDate();
            items.Add(new CatchUpItem("Income", income.Name, from, income.EstimatedAmount, $"Next payday → {income.NextPaymentDate:MMM d}"));
            if (apply) await incomeRepo.UpdateAsync(income);
        }

        // 5. Sinking funds due before the cutoff → due date rolled forward by whole periods (saved amount kept).
        var funds = (await fundRepo.GetAllAsync(userId, activeOnly: true)).ToList();
        foreach (var fund in funds.Where(f => f.NextDueDate < cutoff))
        {
            var from = fund.NextDueDate;
            while (fund.NextDueDate < cutoff) fund.NextDueDate = fund.NextDueDate.AddMonths(fund.PeriodMonths);
            items.Add(new CatchUpItem("Sinking funds", fund.Name, from, fund.TargetAmount, $"Next due → {fund.NextDueDate:MMM d, yyyy}"));
            if (apply) await fundRepo.UpdateAsync(fund);
        }

        // 6. Rebuild projections from today so upcoming items are complete again.
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

    /// <summary>Same schedule the Income page pushes: one entry per payday from NextPaymentDate to the horizon.</summary>
    private async Task RepushIncomeAsync(Income income, int userId, int horizonMonths)
    {
        await cfRepo.DeleteProjectedForSourceAsync(income.Id, userId, TransactionCategory.Income);
        var end = DateOnly.FromDateTime(DateTime.Today.AddMonths(horizonMonths));
        var rows = new List<CashFlowTransaction>();
        var probe = new Income { NextPaymentDate = income.NextPaymentDate, Frequency = income.Frequency };
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
