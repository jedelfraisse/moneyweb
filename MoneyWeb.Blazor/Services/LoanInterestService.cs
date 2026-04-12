using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Computes and applies monthly interest transactions for loans that have an interest rate.
/// Interest is calculated on the running balance at the start of each calendar month,
/// using the formula: balance × (annualRate / 12), rounded to 2 decimal places.
/// One Interest transaction is created per month; months that already have one are skipped.
/// </summary>
public class LoanInterestService(ILoanRepository loanRepo)
{
    /// <summary>
    /// Applies missing monthly interest transactions for a single loan up to (but not including)
    /// the current month. Returns the number of transactions created.
    /// </summary>
    public async Task<int> AccrueInterestAsync(Loan loan, int userId)
    {
        if (loan.InterestRate <= 0 || loan.IsSettled) return 0;

        var transactions = (await loanRepo.GetTransactionsAsync(loan.Id, userId)).ToList();
        return await ApplyMissingInterestAsync(loan, transactions, userId);
    }

    /// <summary>
    /// Runs AccrueInterestAsync for every active loan with an interest rate across all users.
    /// Called by the background service.
    /// </summary>
    public async Task AccrueAllAsync()
    {
        var loans = await loanRepo.GetAllActiveWithInterestAsync();
        foreach (var loan in loans)
            await AccrueInterestAsync(loan, loan.UserId);
    }

    private async Task<int> ApplyMissingInterestAsync(Loan loan, List<LoanTransaction> existingTx, int userId)
    {
        // Reconstruct the original principal before any Additional/Interest/Fee transactions
        var originalPrincipal = loan.Principal
            - existingTx.Where(t => t.Type != LoanTransactionType.Payment).Sum(t => t.Amount);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var startMonth = new DateOnly(loan.LoanDate.Year, loan.LoanDate.Month, 1);
        var endMonth = new DateOnly(today.Year, today.Month, 1); // exclusive — don't charge current month yet

        int count = 0;
        for (var month = startMonth; month < endMonth; month = month.AddMonths(1))
        {
            bool alreadyApplied = existingTx.Any(t =>
                t.Type == LoanTransactionType.Interest &&
                t.TransactionDate.Year == month.Year &&
                t.TransactionDate.Month == month.Month);

            if (alreadyApplied) continue;

            decimal balance = ComputeBalanceAt(originalPrincipal, existingTx, month);
            if (balance <= 0) continue;

            decimal interest = Math.Round(balance * (loan.InterestRate / 12m), 2);
            if (interest <= 0) continue;

            var interestDate = new DateOnly(month.Year, month.Month,
                DateTime.DaysInMonth(month.Year, month.Month));

            var tx = new LoanTransaction
            {
                LoanId = loan.Id,
                UserId = userId,
                TransactionDate = interestDate,
                Type = LoanTransactionType.Interest,
                Amount = interest,
                Notes = $"Monthly interest ({(loan.InterestRate * 100m).ToString("0.##")}% annual)"
            };

            await loanRepo.AddTransactionAsync(tx);
            existingTx.Add(tx); // keep in-memory list current so subsequent months see this tx

            loan.Principal += interest;
            count++;
        }

        if (count > 0)
            await loanRepo.UpdateAsync(loan);

        return count;
    }

    private static decimal ComputeBalanceAt(decimal originalPrincipal, List<LoanTransaction> transactions, DateOnly beforeDate)
    {
        decimal balance = originalPrincipal;
        foreach (var tx in transactions
            .Where(t => t.TransactionDate < beforeDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Id))
        {
            balance = tx.Type == LoanTransactionType.Payment
                ? balance - tx.Amount
                : balance + tx.Amount;
        }
        return balance;
    }
}
