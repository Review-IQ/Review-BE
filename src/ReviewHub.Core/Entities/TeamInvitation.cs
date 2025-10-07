namespace ReviewHub.Core.Entities;

public class TeamInvitation
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public int InvitedByUserId { get; set; }
    public User InvitedBy { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member"; // Owner, Admin, Member
    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public int? AcceptedByUserId { get; set; }
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}
