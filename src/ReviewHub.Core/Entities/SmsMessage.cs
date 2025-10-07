namespace ReviewHub.Core.Entities;

public class SmsMessage
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;

    // Twilio
    public string? TwilioMessageSid { get; set; }
    public string Status { get; set; } = string.Empty; // queued, sent, delivered, failed
    public string? ErrorMessage { get; set; }

    // Campaign
    public string? CampaignName { get; set; }
    public string Purpose { get; set; } = string.Empty; // ReviewRequest, FollowUp, Promotion

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    // Navigation
    public Business Business { get; set; } = null!;
}
