using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        ApplicationDbContext context,
        ILogger<AnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("overview/{businessId}")]
    public async Task<IActionResult> GetOverview(int businessId, [FromQuery] int? locationId = null)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var reviewsQuery = _context.Reviews.Where(r => r.BusinessId == businessId);

            if (locationId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.LocationId == locationId.Value);
            }

            var reviews = await reviewsQuery.ToListAsync();

            var totalReviews = reviews.Count;
            var averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            var positiveReviews = reviews.Count(r => r.Sentiment == "Positive");
            var neutralReviews = reviews.Count(r => r.Sentiment == "Neutral");
            var negativeReviews = reviews.Count(r => r.Sentiment == "Negative");

            // Calculate response rate
            var respondedReviews = reviews.Count(r => !string.IsNullOrEmpty(r.ResponseText));
            var responseRate = totalReviews > 0 ? Math.Round((double)respondedReviews / totalReviews * 100, 1) : 0;

            // Get this month's reviews
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var thisMonthReviews = reviews.Count(r => r.ReviewDate.Month == currentMonth && r.ReviewDate.Year == currentYear);

            // Calculate month-over-month change
            var lastMonth = DateTime.UtcNow.AddMonths(-1).Month;
            var lastMonthYear = DateTime.UtcNow.AddMonths(-1).Year;
            var lastMonthReviews = reviews.Count(r => r.ReviewDate.Month == lastMonth && r.ReviewDate.Year == lastMonthYear);
            var monthlyChange = lastMonthReviews > 0
                ? Math.Round((double)(thisMonthReviews - lastMonthReviews) / lastMonthReviews * 100, 1)
                : 0;

            return Ok(new
            {
                totalReviews = totalReviews,
                averageRating = averageRating,
                responseRate = responseRate,
                sentimentBreakdown = new
                {
                    positive = positiveReviews,
                    neutral = neutralReviews,
                    negative = negativeReviews
                },
                thisMonthReviews = thisMonthReviews,
                monthlyChange = monthlyChange
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics overview");
            return StatusCode(500, new { message = "Failed to get analytics overview" });
        }
    }

    [HttpGet("rating-trend/{businessId}")]
    public async Task<IActionResult> GetRatingTrend(int businessId, [FromQuery] int months = 6, [FromQuery] int? locationId = null)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var startDate = DateTime.UtcNow.AddMonths(-months);
            var reviewsQuery = _context.Reviews
                .Where(r => r.BusinessId == businessId && r.ReviewDate >= startDate);

            if (locationId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.LocationId == locationId.Value);
            }

            var reviews = await reviewsQuery
                .OrderBy(r => r.ReviewDate)
                .ToListAsync();

            // Group by month and calculate average rating
            var trendData = reviews
                .GroupBy(r => new { r.ReviewDate.Year, r.ReviewDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                    averageRating = Math.Round(g.Average(r => r.Rating), 1),
                    reviewCount = g.Count()
                })
                .ToList();

            return Ok(new { trendData = trendData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating trend");
            return StatusCode(500, new { message = "Failed to get rating trend" });
        }
    }

    [HttpGet("platform-breakdown/{businessId}")]
    public async Task<IActionResult> GetPlatformBreakdown(int businessId, [FromQuery] int? locationId = null)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var reviewsQuery = _context.Reviews.Where(r => r.BusinessId == businessId);

            if (locationId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.LocationId == locationId.Value);
            }

            var reviews = await reviewsQuery.ToListAsync();

            var platformBreakdown = reviews
                .GroupBy(r => r.Platform)
                .Select(g => new
                {
                    platform = g.Key,
                    totalReviews = g.Count(),
                    averageRating = Math.Round(g.Average(r => r.Rating), 1),
                    positiveCount = g.Count(r => r.Sentiment == "Positive"),
                    neutralCount = g.Count(r => r.Sentiment == "Neutral"),
                    negativeCount = g.Count(r => r.Sentiment == "Negative")
                })
                .OrderByDescending(p => p.totalReviews)
                .ToList();

            return Ok(new { platformBreakdown = platformBreakdown });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform breakdown");
            return StatusCode(500, new { message = "Failed to get platform breakdown" });
        }
    }

    [HttpGet("sentiment-analysis/{businessId}")]
    public async Task<IActionResult> GetSentimentAnalysis(int businessId, [FromQuery] int days = 30, [FromQuery] int? locationId = null)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var startDate = DateTime.UtcNow.AddDays(-days);
            var reviewsQuery = _context.Reviews
                .Where(r => r.BusinessId == businessId && r.ReviewDate >= startDate);

            if (locationId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.LocationId == locationId.Value);
            }

            var reviews = await reviewsQuery.ToListAsync();

            var sentimentData = reviews
                .GroupBy(r => r.ReviewDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    positive = g.Count(r => r.Sentiment == "Positive"),
                    neutral = g.Count(r => r.Sentiment == "Neutral"),
                    negative = g.Count(r => r.Sentiment == "Negative"),
                    averageSentimentScore = Math.Round(g.Average(r => r.SentimentScore ?? 0), 2)
                })
                .ToList();

            var overallSentiment = new
            {
                positive = reviews.Count(r => r.Sentiment == "Positive"),
                neutral = reviews.Count(r => r.Sentiment == "Neutral"),
                negative = reviews.Count(r => r.Sentiment == "Negative"),
                averageScore = reviews.Any() ? Math.Round(reviews.Average(r => r.SentimentScore ?? 0), 2) : 0
            };

            return Ok(new
            {
                sentimentTrend = sentimentData,
                overallSentiment = overallSentiment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sentiment analysis");
            return StatusCode(500, new { message = "Failed to get sentiment analysis" });
        }
    }

    [HttpGet("top-keywords/{businessId}")]
    public async Task<IActionResult> GetTopKeywords(int businessId, [FromQuery] int limit = 10)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .Select(r => r.ReviewText)
                .ToListAsync();

            // Simple keyword extraction (can be enhanced with NLP libraries)
            var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from", "is", "was", "are", "were", "been", "be", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "this", "that", "these", "those", "i", "you", "he", "she", "it", "we", "they", "my", "your", "his", "her", "its", "our", "their", "very", "really", "just", "so" };

            var allWords = reviews
                .SelectMany(r => r.ToLower().Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(w => w.Length > 3 && !stopWords.Contains(w))
                .ToList();

            var keywords = allWords
                .GroupBy(w => w)
                .Select(g => new
                {
                    keyword = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(k => k.count)
                .Take(limit)
                .ToList();

            return Ok(new { keywords = keywords });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top keywords");
            return StatusCode(500, new { message = "Failed to get top keywords" });
        }
    }

    [HttpGet("response-time/{businessId}")]
    public async Task<IActionResult> GetResponseTimeMetrics(int businessId)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && r.ResponseDate != null)
                .ToListAsync();

            var responseTimes = reviews
                .Where(r => r.ResponseDate.HasValue)
                .Select(r => (r.ResponseDate!.Value - r.ReviewDate).TotalHours)
                .ToList();

            var averageResponseTime = responseTimes.Any()
                ? Math.Round(responseTimes.Average(), 1)
                : 0;

            var medianResponseTime = 0.0;
            if (responseTimes.Any())
            {
                var sortedTimes = responseTimes.OrderBy(t => t).ToList();
                var mid = sortedTimes.Count / 2;
                medianResponseTime = sortedTimes.Count % 2 == 0
                    ? Math.Round((sortedTimes[mid - 1] + sortedTimes[mid]) / 2, 1)
                    : Math.Round(sortedTimes[mid], 1);
            }

            var within24Hours = responseTimes.Count(t => t <= 24);
            var within24HoursPercentage = responseTimes.Any()
                ? Math.Round((double)within24Hours / responseTimes.Count * 100, 1)
                : 0;

            return Ok(new
            {
                averageResponseTimeHours = averageResponseTime,
                medianResponseTimeHours = medianResponseTime,
                totalResponses = responseTimes.Count,
                within24Hours = within24Hours,
                within24HoursPercentage = within24HoursPercentage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting response time metrics");
            return StatusCode(500, new { message = "Failed to get response time metrics" });
        }
    }

    [HttpGet("dashboard-summary/{businessId}")]
    public async Task<IActionResult> GetDashboardSummary(int businessId, [FromQuery] int? locationId = null)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var reviewsQuery = _context.Reviews.Where(r => r.BusinessId == businessId);

            if (locationId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.LocationId == locationId.Value);
            }

            var reviews = await reviewsQuery.ToListAsync();

            var totalReviews = reviews.Count;
            var averageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
            var unreadReviews = reviews.Count(r => !r.IsRead);

            // Get platform connections
            var connectedPlatforms = await _context.PlatformConnections
                .Where(p => p.BusinessId == businessId && p.IsActive)
                .CountAsync();

            // Get SMS usage
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var smsUsage = await _context.SmsMessages
                .Where(s => s.BusinessId == businessId &&
                            s.SentAt.Month == currentMonth &&
                            s.SentAt.Year == currentYear)
                .CountAsync();

            var plan = user.SubscriptionPlan ?? "Free";
            var smsLimit = plan switch
            {
                "Pro" => 500,
                "Enterprise" => int.MaxValue,
                _ => 10
            };

            // Get recent activity
            var recentReviews = reviews
                .OrderByDescending(r => r.ReviewDate)
                .Take(5)
                .Select(r => new
                {
                    r.Id,
                    r.Platform,
                    r.ReviewerName,
                    r.Rating,
                    r.ReviewDate
                })
                .ToList();

            return Ok(new
            {
                totalReviews = totalReviews,
                averageRating = averageRating,
                unreadReviews = unreadReviews,
                connectedPlatforms = connectedPlatforms,
                smsUsage = new
                {
                    sent = smsUsage,
                    limit = smsLimit,
                    remaining = smsLimit - smsUsage
                },
                subscriptionPlan = plan,
                recentReviews = recentReviews
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, new { message = "Failed to get dashboard summary" });
        }
    }
}
