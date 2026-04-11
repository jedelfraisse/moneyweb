namespace MoneyWeb.Data.Models;

public enum ShareEntityType
{
    Debt = 0,
    Income = 1,
    BankAccount = 2,
    Bill = 3,
    Loan = 4
}

public class SharePermission
{
    public int Id { get; set; }
    public ShareEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public int GrantedByUserId { get; set; }

    // Exactly one of these is non-null
    public int? SharedWithUserId { get; set; }
    public int? SharedWithGroupId { get; set; }

    // Display (populated by join queries)
    public string? SharedWithDisplayName { get; set; }  // user DisplayName or group Name
    public bool IsGroup => SharedWithGroupId.HasValue;

    public DateTime CreatedAt { get; set; }
}
