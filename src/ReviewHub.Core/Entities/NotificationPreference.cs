namespace ReviewHub.Core.Entities;

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = false;
    public bool SmsNotifications { get; set; } = false;

    // Granular controls
    public bool NotifyOnNewReview { get; set; } = true;
    public bool NotifyOnReviewReply { get; set; } = true;
    public bool NotifyOnLowRating { get; set; } = true;  // e.g., 1-2 stars

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
