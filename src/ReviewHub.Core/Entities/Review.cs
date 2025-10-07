using ReviewHub.Core.Enums;

namespace ReviewHub.Core.Entities;

public class Review
{
    public int Id { get; set; }
    public int BusinessId { get; set; } // Legacy - kept for backward compatibility
    public int? LocationId { get; set; } // NEW: Which specific location this review belongs to
    public ReviewPlatform Platform { get; set; }
    public string PlatformReviewId { get; set; } = string.Empty;

    // Reviewer info
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerEmail { get; set; }
    public string? ReviewerAvatarUrl { get; set; }

    // Review content
    public int Rating { get; set; } // 1-5 stars
    public string? ReviewText { get; set; }
    public DateTime ReviewDate { get; set; }

    // Response
    public string? ResponseText { get; set; }
    public DateTime? ResponseDate { get; set; }
    public int? ResponderId { get; set; } // User who responded
    public bool IsAutoReplied { get; set; } = false;

    // AI & Sentiment
    public string? Sentiment { get; set; } // Positive, Neutral, Negative
    public double? SentimentScore { get; set; } // -1.0 to 1.0
    public string? AiSuggestedResponse { get; set; }
    public string? Tags { get; set; } // JSON array of tags

    // Metadata
    public bool IsRead { get; set; } = false;
    public bool IsFlagged { get; set; } = false;
    public string? Location { get; set; } // Text description (deprecated, use LocationId)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Business Business { get; set; } = null!;
    public Entities.Location? ReviewLocation { get; set; } // NEW: Reference to Location entity
}
