using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ISinkingFundRepository
{
    Task<IEnumerable<SinkingFund>> GetAllAsync(int userId, bool activeOnly = true);
    Task<int> CreateAsync(SinkingFund fund);
    Task UpdateAsync(SinkingFund fund);
    Task DeleteAsync(int id, int userId);
    /// <summary>Add amount to CurrentSaved (can be negative for withdrawals).</summary>
    Task AdjustSavedAsync(int id, int userId, decimal delta);
    /// <summary>Reset CurrentSaved to 0 and advance NextDueDate by one period (called when the bill is paid).</summary>
    Task MarkPaidAndResetAsync(int id, int userId);
}
