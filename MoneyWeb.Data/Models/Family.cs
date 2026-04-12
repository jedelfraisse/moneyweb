namespace MoneyWeb.Data.Models;

public enum ChoreFrequency
{
    OneTime = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public enum KidTransactionSource
{
    Deposit = 0,
    Withdrawal = 1,
    ChoreReward = 2
}

public class Kid
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ColorHex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Chore
{
    public int Id { get; set; }
    public int KidId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal RewardAmount { get; set; }
    public ChoreFrequency Frequency { get; set; } = ChoreFrequency.Weekly;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ChoreCompletion
{
    public int Id { get; set; }
    public int ChoreId { get; set; }
    public int KidId { get; set; }
    public int UserId { get; set; }
    public DateOnly CompletedDate { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class KidTransaction
{
    public int Id { get; set; }
    public int KidId { get; set; }
    public int UserId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ContributorName { get; set; }
    public KidTransactionSource Source { get; set; } = KidTransactionSource.Deposit;
    public int? ChoreCompletionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
