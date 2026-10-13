using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Services;

public interface ISinkingFundProjectionService
{
    /// <summary>Push/refresh projected CF transactions for a Sinking Fund — a no-op if it has neither
    /// account set (nothing to project into).</summary>
    Task PushAsync(SinkingFund fund, int userId, int horizonMonths);
}

/// <summary>
/// Generates projected cash-flow transactions for a Sinking Fund:
/// - Both BankAccountId (Savings Account) and FundingAccountId (Transfer From) set → a paired monthly
///   transfer, outflow from the funding account and inflow to the savings account.
/// - Only one of the two set → a single "reserve" reminder transaction against whichever account it is —
///   BankAccountId when there's a bucket but no distinct source, or FundingAccountId when there's a
///   source but no separate savings bucket yet (e.g. a debt fee just linked to "what pays for this,"
///   with no transfer destination configured). Either way it's the same reservation semantics; only the
///   account differs.
/// Plus the lump-sum payment transaction on each due date within the horizon, against BankAccountId if
/// set (the money should land in the bucket by then) or FundingAccountId otherwise.
///
/// Extracted from SinkingFunds.razor so the Debts page (linking a fee to a newly created fund) can
/// trigger the same push instead of creating a fund that silently never shows up anywhere in cash flow.
/// </summary>
public class SinkingFundProjectionService(ICashFlowRepository cfRepo) : ISinkingFundProjectionService
{
    public async Task PushAsync(SinkingFund fund, int userId, int horizonMonths)
    {
        if (!fund.BankAccountId.HasValue && !fund.FundingAccountId.HasValue) return;

        await cfRepo.DeleteProjectedForSourceAsync(fund.Id, userId, TransactionCategory.SinkingFundPayment);
        await cfRepo.DeleteProjectedForSourceAsync(fund.Id, userId, TransactionCategory.SinkingFundContribution);

        var transactions = new List<CashFlowTransaction>();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddMonths(horizonMonths);

        // Both accounts set → a real transfer. Otherwise, whichever single account is actually set is
        // where the reservation reminder and the lump-sum payment post.
        var isRealTransfer = fund.BankAccountId.HasValue && fund.FundingAccountId.HasValue;
        var soloAccountId = fund.BankAccountId ?? fund.FundingAccountId!.Value;

        // Monthly contribution transfers within horizon
        var contribDate = today.AddMonths(1);
        contribDate = new DateOnly(contribDate.Year, contribDate.Month, 1); // first of month
        while (contribDate <= cutoff && contribDate < fund.NextDueDate)
        {
            if (isRealTransfer)
            {
                // Outflow from funding (source) account
                transactions.Add(new CashFlowTransaction
                {
                    UserId = userId,
                    BankAccountId = fund.FundingAccountId!.Value,
                    TransactionDate = contribDate,
                    Description = $"Sinking Fund: {fund.Name} – transfer to savings",
                    Amount = -fund.EffectiveMonthlyContribution,
                    Category = TransactionCategory.SinkingFundContribution,
                    ReferenceId = fund.Id,
                    IsProjected = true
                });
                // Inflow to savings (destination) account
                transactions.Add(new CashFlowTransaction
                {
                    UserId = userId,
                    BankAccountId = fund.BankAccountId!.Value,
                    TransactionDate = contribDate,
                    Description = $"Sinking Fund: {fund.Name} – contribution received",
                    Amount = fund.EffectiveMonthlyContribution,
                    Category = TransactionCategory.SinkingFundContribution,
                    ReferenceId = fund.Id,
                    IsProjected = true
                });
            }
            else
            {
                // Single-account bucket: show as a reservation reminder (negative = reserved/locked)
                transactions.Add(new CashFlowTransaction
                {
                    UserId = userId,
                    BankAccountId = soloAccountId,
                    TransactionDate = contribDate,
                    Description = $"Sinking Fund: {fund.Name} – reserve",
                    Amount = -fund.EffectiveMonthlyContribution,
                    Category = TransactionCategory.SinkingFundContribution,
                    ReferenceId = fund.Id,
                    IsProjected = true
                });
            }
            contribDate = contribDate.AddMonths(1);
        }

        // Lump-sum payment dates within horizon — lands in the bucket if there is one, otherwise the
        // one account this fund has (soloAccountId already prefers BankAccountId when both are set).
        var payDate = fund.NextDueDate;
        while (payDate <= cutoff)
        {
            transactions.Add(new CashFlowTransaction
            {
                UserId = userId,
                BankAccountId = soloAccountId,
                TransactionDate = payDate,
                Description = $"Sinking Fund Payment: {fund.Name}",
                Amount = -fund.TargetAmount,
                Category = TransactionCategory.SinkingFundPayment,
                ReferenceId = fund.Id,
                IsProjected = true
            });
            payDate = payDate.AddMonths(fund.PeriodMonths);
        }

        if (transactions.Count > 0)
            await cfRepo.BulkInsertProjectedAsync(transactions);
    }
}
