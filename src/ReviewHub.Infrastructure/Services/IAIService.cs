using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface IAIService
{
    // Review response generation
    Task<string> GenerateReviewResponseAsync(Review review, Business business, string? tone = null, string? length = null);
    Task<bool> ShouldAutoReplyAsync(Review review, AISettings settings);

    // Sentiment and analysis
    Task<string> AnalyzeSentimentAsync(string text);
    Task<List<string>> ExtractKeywordsAsync(string text);
    Task<bool> ContainsQuestionAsync(string text);

    // Insights generation
    Task<string> GenerateReviewSummaryAsync(List<Review> reviews);
    Task<string> GenerateCompetitorInsightsAsync(Business business, List<Competitor> competitors);
    Task<string> GenerateAnalyticsInsightsAsync(Business business, object analyticsData);
    Task<List<string>> GenerateActionableRecommendationsAsync(Business business, List<Review> recentReviews);

    // Content generation
    Task<string> GenerateSocialMediaPostAsync(Review review, string platform);
    Task<string> ImproveReviewResponseAsync(string originalResponse, Review review);
}
