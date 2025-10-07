using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Infrastructure.Data;
using ReviewHub.Infrastructure.Services;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        ApplicationDbContext context,
        IAIService aiService,
        ILogger<AIController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get AI settings for the current user
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetAISettings()
    {
        try
        {
            var userId = GetUserId();
            var settings = await _context.AISettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                // Create default settings
                settings = new Core.Entities.AISettings
                {
                    UserId = userId,
                    EnableAutoReply = false,
                    AutoReplyToPositiveReviews = true,
                    AutoReplyToNeutralReviews = false,
                    AutoReplyToNegativeReviews = false,
                    AutoReplyToQuestions = true,
                    EnableAISuggestions = true,
                    EnableSentimentAnalysis = true,
                    EnableCompetitorAnalysis = true,
                    EnableInsightsGeneration = true,
                    ResponseTone = "Professional",
                    ResponseLength = "Medium",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.AISettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI settings");
            return StatusCode(500, new { message = "Error retrieving AI settings" });
        }
    }

    /// <summary>
    /// Update AI settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateAISettings([FromBody] UpdateAISettingsRequest request)
    {
        try
        {
            var userId = GetUserId();
            var settings = await _context.AISettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new Core.Entities.AISettings { UserId = userId, CreatedAt = DateTime.UtcNow };
                _context.AISettings.Add(settings);
            }

            settings.EnableAutoReply = request.EnableAutoReply;
            settings.AutoReplyToPositiveReviews = request.AutoReplyToPositiveReviews;
            settings.AutoReplyToNeutralReviews = request.AutoReplyToNeutralReviews;
            settings.AutoReplyToNegativeReviews = false; // Always false for safety
            settings.AutoReplyToQuestions = request.AutoReplyToQuestions;
            settings.EnableAISuggestions = request.EnableAISuggestions;
            settings.EnableSentimentAnalysis = request.EnableSentimentAnalysis;
            settings.EnableCompetitorAnalysis = request.EnableCompetitorAnalysis;
            settings.EnableInsightsGeneration = request.EnableInsightsGeneration;
            settings.ResponseTone = request.ResponseTone;
            settings.ResponseLength = request.ResponseLength;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI settings");
            return StatusCode(500, new { message = "Error updating AI settings" });
        }
    }

    /// <summary>
    /// Generate AI response suggestion for a review
    /// </summary>
    [HttpPost("generate-response/{reviewId}")]
    public async Task<IActionResult> GenerateResponse(int reviewId)
    {
        try
        {
            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            var userId = GetUserId();
            var settings = await _context.AISettings.FirstOrDefaultAsync(s => s.UserId == userId);

            var response = await _aiService.GenerateReviewResponseAsync(
                review,
                review.Business,
                settings?.ResponseTone,
                settings?.ResponseLength
            );

            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            return StatusCode(500, new { message = "Error generating response" });
        }
    }

    /// <summary>
    /// Improve an existing response
    /// </summary>
    [HttpPost("improve-response/{reviewId}")]
    public async Task<IActionResult> ImproveResponse(int reviewId, [FromBody] ImproveResponseRequest request)
    {
        try
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            var improvedResponse = await _aiService.ImproveReviewResponseAsync(request.OriginalResponse, review);

            return Ok(new { response = improvedResponse });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving response");
            return StatusCode(500, new { message = "Error improving response" });
        }
    }

    /// <summary>
    /// Generate analytics insights
    /// </summary>
    [HttpGet("insights/analytics/{businessId}")]
    public async Task<IActionResult> GetAnalyticsInsights(int businessId)
    {
        try
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Get analytics data (simplified for this example)
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync();

            var analyticsData = new
            {
                TotalReviews = reviews.Count,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                RatingDistribution = reviews.GroupBy(r => r.Rating)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentReviews = reviews.Take(10).Select(r => new { r.Rating, r.ReviewText })
            };

            var insights = await _aiService.GenerateAnalyticsInsightsAsync(business, analyticsData);

            return Ok(new { insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics insights");
            return StatusCode(500, new { message = "Error generating insights" });
        }
    }

    /// <summary>
    /// Generate competitor insights
    /// </summary>
    [HttpGet("insights/competitors/{businessId}")]
    public async Task<IActionResult> GetCompetitorInsights(int businessId)
    {
        try
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var competitors = await _context.Competitors
                .Where(c => c.BusinessId == businessId)
                .ToListAsync();

            if (!competitors.Any())
            {
                return Ok(new { insights = "No competitor data available. Add competitors to get AI-powered insights." });
            }

            var insights = await _aiService.GenerateCompetitorInsightsAsync(business, competitors);

            return Ok(new { insights });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating competitor insights");
            return StatusCode(500, new { message = "Error generating insights" });
        }
    }

    /// <summary>
    /// Generate review summary
    /// </summary>
    [HttpGet("insights/review-summary/{businessId}")]
    public async Task<IActionResult> GetReviewSummary(int businessId, [FromQuery] int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && r.CreatedAt >= cutoffDate)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (!reviews.Any())
            {
                return Ok(new { summary = $"No reviews found in the last {days} days." });
            }

            var summary = await _aiService.GenerateReviewSummaryAsync(reviews);

            return Ok(new { summary, reviewCount = reviews.Count, period = $"{days} days" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating review summary");
            return StatusCode(500, new { message = "Error generating summary" });
        }
    }

    /// <summary>
    /// Generate actionable recommendations
    /// </summary>
    [HttpGet("insights/recommendations/{businessId}")]
    public async Task<IActionResult> GetRecommendations(int businessId)
    {
        try
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var recentReviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            var recommendations = await _aiService.GenerateActionableRecommendationsAsync(business, recentReviews);

            return Ok(new { recommendations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            return StatusCode(500, new { message = "Error generating recommendations" });
        }
    }

    /// <summary>
    /// Generate social media post from review
    /// </summary>
    [HttpPost("generate-social-post/{reviewId}")]
    public async Task<IActionResult> GenerateSocialPost(int reviewId, [FromQuery] string platform = "Twitter")
    {
        try
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            if (review.Rating < 4)
            {
                return BadRequest(new { message = "Social media posts are only generated for positive reviews (4-5 stars)" });
            }

            var post = await _aiService.GenerateSocialMediaPostAsync(review, platform);

            return Ok(new { post, platform });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating social media post");
            return StatusCode(500, new { message = "Error generating post" });
        }
    }
}

public record UpdateAISettingsRequest(
    bool EnableAutoReply,
    bool AutoReplyToPositiveReviews,
    bool AutoReplyToNeutralReviews,
    bool AutoReplyToQuestions,
    bool EnableAISuggestions,
    bool EnableSentimentAnalysis,
    bool EnableCompetitorAnalysis,
    bool EnableInsightsGeneration,
    string ResponseTone,
    string ResponseLength
);

public record ImproveResponseRequest(string OriginalResponse);
