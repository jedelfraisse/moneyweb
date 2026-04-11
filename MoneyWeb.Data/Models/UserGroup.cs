namespace MoneyWeb.Data.Models;

public enum GroupMemberStatus
{
    Pending = 0,
    Active = 1
}

public class UserGroup
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<UserGroupMember> Members { get; set; } = [];
    public string? OwnerDisplayName { get; set; }  // populated by join
}

public class UserGroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int? UserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string? DisplayName { get; set; }   // from Users join
    public GroupMemberStatus Status { get; set; }
    public bool CanSeeAll { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? JoinedAt { get; set; }
}
