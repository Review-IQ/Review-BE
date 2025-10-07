namespace ReviewHub.Core.Entities;

public class BusinessUser
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Role { get; set; } = "Member"; // Owner, Admin, Member
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
