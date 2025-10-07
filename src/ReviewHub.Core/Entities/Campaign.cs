namespace ReviewHub.Core.Entities;

public class Campaign
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Scheduled, Sending, Sent, Failed
    public int SentCount { get; set; } = 0;
    public int TotalRecipients { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Business Business { get; set; } = null!;
}
