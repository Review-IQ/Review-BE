namespace ReviewHub.Core.Entities;

public class AISettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Auto-reply settings
    public bool EnableAutoReply { get; set; } = false;
    public bool AutoReplyToPositiveReviews { get; set; } = true;
    public bool AutoReplyToNeutralReviews { get; set; } = false;
    public bool AutoReplyToNegativeReviews { get; set; } = false; // Always false by default
    public bool AutoReplyToQuestions { get; set; } = true;

    // AI assistance settings
    public bool EnableAISuggestions { get; set; } = true;
    public bool EnableSentimentAnalysis { get; set; } = true;
    public bool EnableCompetitorAnalysis { get; set; } = true;
    public bool EnableInsightsGeneration { get; set; } = true;

    // Tone and style
    public string ResponseTone { get; set; } = "Professional"; // Professional, Friendly, Casual
    public string ResponseLength { get; set; } = "Medium"; // Short, Medium, Long

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
