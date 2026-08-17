namespace MoneyWeb.Data.Services;

public interface IDebtProjectionService
{
    /// <summary>Push/refresh projected CF transactions for a debt (handles grouped and ungrouped).</summary>
    Task PushDebtAsync(int debtId, int userId);

    /// <summary>Push/refresh projected CF transactions for all debts in a group.</summary>
    Task PushGroupAsync(int groupId, int userId, bool includeManualOverrides = false);
}
