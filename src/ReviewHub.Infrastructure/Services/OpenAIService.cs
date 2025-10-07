using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ReviewHub.Core.Entities;
using System.ClientModel;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class OpenAIService : IAIService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _model;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _logger = logger;
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var client = new AzureOpenAIClient(new Uri("https://api.openai.com/v1"), new ApiKeyCredential(apiKey));
        _chatClient = client.GetChatClient(_model);
    }

    public async Task<string> GenerateReviewResponseAsync(Review review, Business business, string? tone = null, string? length = null)
    {
        tone ??= "Professional";
        length ??= "Medium";

        var lengthGuidance = length switch
        {
            "Short" => "Keep the response brief, 1-2 sentences.",
            "Long" => "Provide a detailed, thoughtful response of 4-5 sentences.",
            _ => "Provide a response of 2-3 sentences."
        };

        var toneGuidance = tone switch
        {
            "Friendly" => "Use a warm, friendly, and conversational tone.",
            "Casual" => "Use a casual, relaxed tone while remaining respectful.",
            _ => "Use a professional and courteous tone."
        };

        var sentiment = review.Rating >= 4 ? "positive" : review.Rating == 3 ? "neutral" : "negative";
        var prompt = $@"You are a customer service representative for {business.Name}, a {business.Industry ?? "business"}.

Customer Review ({review.Rating} stars - {sentiment}):
{review.ReviewText ?? "No review text provided."}

Generate a personalized response to this review. Guidelines:
- {toneGuidance}
- {lengthGuidance}
- Thank them for their feedback
- Address specific points they mentioned
- {(sentiment == "positive" ? "Express gratitude and encourage them to visit again" : sentiment == "neutral" ? "Address their concerns and offer to improve" : "Apologize sincerely and offer to resolve the issue")}
- Include the business name naturally
- Be authentic and personal, not robotic
- Do not use placeholders like [Business Name] - use the actual name: {business.Name}

Response:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an expert customer service professional who writes empathetic, personalized review responses."),
                new UserChatMessage(prompt)
            };

            var response = await _chatClient.CompleteChatAsync(messages);

            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating review response");
            throw;
        }
    }

    public async Task<bool> ShouldAutoReplyAsync(Review review, AISettings settings)
    {
        if (!settings.EnableAutoReply) return false;

        // Never auto-reply to negative reviews
        if (review.Rating <= 2) return false;

        // Check rating-based rules
        if (review.Rating >= 4 && !settings.AutoReplyToPositiveReviews) return false;
        if (review.Rating == 3 && !settings.AutoReplyToNeutralReviews) return false;

        // Check if it contains a question
        if (settings.AutoReplyToQuestions && !string.IsNullOrEmpty(review.ReviewText) && await ContainsQuestionAsync(review.ReviewText))
        {
            return true;
        }

        // Standard auto-reply based on rating
        return review.Rating >= 4 && settings.AutoReplyToPositiveReviews;
    }

    public async Task<string> AnalyzeSentimentAsync(string text)
    {
        var prompt = $@"Analyze the sentiment of this text and respond with only ONE word: Positive, Neutral, or Negative.

Text: {text}

Sentiment:";

        try
        {
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return "Neutral";
        }
    }

    public async Task<List<string>> ExtractKeywordsAsync(string text)
    {
        var prompt = $@"Extract the 5-10 most important keywords or phrases from this text. Return them as a JSON array of strings.

Text: {text}

Keywords (JSON array):";

        try
        {
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var response = await _chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text.Trim();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting keywords");
            return new List<string>();
        }
    }

    public async Task<bool> ContainsQuestionAsync(string text)
    {
        var prompt = $@"Does this text contain a question that requires a response? Answer with only 'yes' or 'no'.

Text: {text}

Answer:";

        try
        {
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var response = await _chatClient.CompleteChatAsync(messages);
            var answer = response.Value.Content[0].Text.Trim().ToLower();
            return answer.Contains("yes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for questions");
            return false;
        }
    }

    public async Task<string> GenerateReviewSummaryAsync(List<Review> reviews)
    {
        if (!reviews.Any()) return "No reviews available for analysis.";

        var reviewsText = string.Join("\n\n", reviews.Take(50).Select(r =>
            $"[{r.Rating} stars] {r.ReviewText ?? "No text"}"));

        var prompt = $@"Analyze these customer reviews and provide a comprehensive summary including:
1. Overall sentiment and trends
2. Most common positive themes
3. Most common concerns or issues
4. Key areas for improvement
5. Notable standout feedback

Reviews:
{reviewsText}

Summary:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a business analyst expert at summarizing customer feedback."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating review summary");
            throw;
        }
    }

    public async Task<string> GenerateCompetitorInsightsAsync(Business business, List<Competitor> competitors)
    {
        if (!competitors.Any()) return "No competitor data available for analysis.";

        var competitorData = string.Join("\n", competitors.Select(c =>
            $"- {c.Name}: {c.CurrentRating:F1} stars, {c.TotalReviews ?? 0} reviews"));

        var prompt = $@"Analyze this competitive landscape for {business.Name} and provide strategic insights:

Your Business:
- Name: {business.Name}
- Average Rating: [Your current rating]

Competitors:
{competitorData}

Provide:
1. Competitive position analysis
2. Key differentiators or gaps
3. Opportunities to improve
4. Actionable recommendations

Analysis:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a competitive intelligence analyst specializing in local business strategy."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating competitor insights");
            throw;
        }
    }

    public async Task<string> GenerateAnalyticsInsightsAsync(Business business, object analyticsData)
    {
        var dataJson = JsonSerializer.Serialize(analyticsData);

        var prompt = $@"Analyze this business analytics data for {business.Name} and provide actionable insights:

Analytics Data:
{dataJson}

Provide:
1. Key performance indicators analysis
2. Trends and patterns identified
3. Areas of concern
4. Growth opportunities
5. 3-5 specific action items

Insights:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a business analytics expert who provides clear, actionable insights."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics insights");
            throw;
        }
    }

    public async Task<List<string>> GenerateActionableRecommendationsAsync(Business business, List<Review> recentReviews)
    {
        if (!recentReviews.Any()) return new List<string> { "Insufficient data for recommendations." };

        var reviewsSummary = string.Join("\n", recentReviews.Take(30).Select(r =>
        {
            var text = r.ReviewText ?? "No text";
            return $"[{r.Rating}★] {text.Substring(0, Math.Min(100, text.Length))}...";
        }));

        var prompt = $@"Based on these recent reviews for {business.Name}, generate 5-7 specific, actionable recommendations to improve customer satisfaction.

Recent Reviews:
{reviewsSummary}

Return the recommendations as a JSON array of strings. Each recommendation should be:
- Specific and actionable
- Based on actual feedback patterns
- Prioritized by impact
- Realistic to implement

JSON array:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a customer experience consultant who provides practical, data-driven recommendations."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text.Trim();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            return new List<string> { "Unable to generate recommendations at this time." };
        }
    }

    public async Task<string> GenerateSocialMediaPostAsync(Review review, string platform)
    {
        var charLimit = platform.ToLower() switch
        {
            "twitter" or "x" => "280 characters",
            "instagram" => "2200 characters but aim for 125-150",
            _ => "no specific limit but keep it concise"
        };

        var prompt = $@"Create an engaging {platform} post celebrating this positive customer review.

Review ({review.Rating} stars):
{review.ReviewText ?? "No text provided"}

Guidelines:
- Character limit: {charLimit}
- Include relevant hashtags
- Encourage engagement
- Maintain brand voice
- Make it shareable
- Don't mention the customer's name

Post:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a social media marketing expert who creates engaging, authentic content."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating social media post");
            throw;
        }
    }

    public async Task<string> ImproveReviewResponseAsync(string originalResponse, Review review)
    {
        var prompt = $@"Improve this review response to make it more effective and personalized.

Original Response:
{originalResponse}

Review ({review.Rating} stars):
{review.ReviewText ?? "No text"}

Provide an improved version that:
- Maintains the key points
- Sounds more natural and less templated
- Is more empathetic and personal
- Addresses specific details from the review
- Has better flow and structure

Improved Response:";

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a customer service expert who refines communication for maximum impact."),
                new UserChatMessage(prompt)
            };
            var response = await _chatClient.CompleteChatAsync(messages);
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving review response");
            throw;
        }
    }
}
