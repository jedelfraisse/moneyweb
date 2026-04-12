using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IKidRepository
{
    // Kids
    Task<IEnumerable<Kid>> GetByUserAsync(int userId);
    Task<Kid?> GetByIdAsync(int id, int userId);
    Task<int> CreateKidAsync(Kid kid);
    Task UpdateKidAsync(Kid kid);
    Task DeleteKidAsync(int id, int userId);

    // Chores
    Task<IEnumerable<Chore>> GetChoresForKidAsync(int kidId, int userId);
    Task<int> CreateChoreAsync(Chore chore);
    Task UpdateChoreAsync(Chore chore);
    Task DeleteChoreAsync(int id, int userId);

    // Transactions
    Task<IEnumerable<KidTransaction>> GetTransactionsAsync(int kidId, int userId);
    Task<int> AddTransactionAsync(KidTransaction tx);
    Task DeleteTransactionAsync(int id, int userId);

    // Chore completion — atomically inserts completion + reward transaction
    Task<int> CompleteChoreAsync(ChoreCompletion completion);

    // Balance helper
    Task<decimal> GetBalanceAsync(int kidId, int userId);
}
