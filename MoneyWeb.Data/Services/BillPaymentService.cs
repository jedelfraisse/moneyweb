using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

public interface IBillPaymentService
{
    /// <summary>
    /// Put an open bill occurrence into a bank account's cash flow as a pending payment linked to that exact
    /// occurrence, so submitting, clearing or skipping it on either the Bills or Transactions page keeps both in
    /// sync. Works for one-offs and for bills with no bank account of their own.
    /// </summary>
    Task AddToCashFlowAsync(BillOccurrence occ, int bankAccountId, DateOnly payDate, decimal amount, int userId);

    /// <summary>
    /// The bill a sinking fund was saving for has arrived: record it as a one-off bill (status Received) linked to
    /// the fund, retarget the fund to the real amount and due date so its monthly savings recalculate, and
    /// optionally schedule the payment in cash flow. The fund stops projecting its own lump-sum payment for this
    /// period — the bill's payment replaces it.
    /// </summary>
    Task<int> RecordSinkingFundBillAsync(SinkingFund fund, decimal amount, DateOnly dueDate,
        int? payFromAccountId, DateOnly? payDate, int userId, int horizonMonths);
}

public class BillPaymentService(
    IBillRepository billRepo,
    ICashFlowRepository cfRepo,
    ISinkingFundRepository fundRepo,
    ISinkingFundProjectionService fundProjection) : IBillPaymentService
{
    public async Task AddToCashFlowAsync(BillOccurrence occ, int bankAccountId, DateOnly payDate, decimal amount, int userId)
    {
        var bill = occ.BillId is { } billId ? await billRepo.GetByIdAsync(billId, userId) : null;

        // Keep the occurrence's planned pay date on the payment's date, so the older bill-and-date matching
        // (used by recurring bills' scheduled entries) lines up with it too.
        var planned = payDate == occ.DueDate ? null : (DateOnly?)payDate;
        if (occ.PlannedPayDate != planned)
        {
            occ.PlannedPayDate = planned;
            await billRepo.UpdateOccurrenceAsync(occ);
        }

        await cfRepo.InsertScheduledPaymentAsync(new CashFlowTransaction
        {
            UserId = userId,
            BankAccountId = bankAccountId,
            TransactionDate = payDate,
            Description = $"Bill: {occ.DisplayName}",
            Amount = -amount,
            Category = TransactionCategory.Bill,
            ReferenceId = occ.BillId,
            BillOccurrenceId = occ.Id,
            IsProjected = true,
            IsAutoDraft = bill?.PaymentMethod == DebtPaymentMethod.AutoDraft,
            IsSubmitted = occ.Status == BillOccurrenceStatus.Submitted
        });
    }

    public async Task<int> RecordSinkingFundBillAsync(SinkingFund fund, decimal amount, DateOnly dueDate,
        int? payFromAccountId, DateOnly? payDate, int userId, int horizonMonths)
    {
        var occ = new BillOccurrence
        {
            UserId = userId,
            BillId = null,
            Name = $"{fund.Name} {dueDate.Year}",
            SinkingFundId = fund.Id,
            DueDate = dueDate,
            EstimatedAmount = amount,
            ActualAmount = amount,
            Status = BillOccurrenceStatus.Received
        };
        occ.Id = await billRepo.CreateOccurrenceAsync(occ);

        // The estimate is replaced by the real bill: save toward its amount and due date from here on.
        fund.TargetAmount = amount;
        fund.NextDueDate = dueDate;
        await fundRepo.UpdateAsync(fund);

        if (payFromAccountId is { } accountId)
            await AddToCashFlowAsync(occ, accountId, payDate ?? dueDate, amount, userId);

        await fundProjection.PushAsync(fund, userId, horizonMonths);
        return occ.Id;
    }
}
