using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get reviews with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviews(
        [FromQuery] int? businessId,
        [FromQuery] int? locationId,
        [FromQuery] string? platform,
        [FromQuery] string? sentiment,
        [FromQuery] int? rating,
        [FromQuery] bool? isRead,
        [FromQuery] bool? isFlagged,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var query = _context.Reviews
                .Include(r => r.Business)
                .Where(r => r.Business.UserId == user.Id);

            // Apply filters
            if (businessId.HasValue)
            {
                query = query.Where(r => r.BusinessId == businessId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(r => r.LocationId == locationId.Value);
            }

            if (!string.IsNullOrEmpty(platform) && Enum.TryParse<ReviewPlatform>(platform, true, out var platformEnum))
            {
                query = query.Where(r => r.Platform == platformEnum);
            }

            if (!string.IsNullOrEmpty(sentiment))
            {
                query = query.Where(r => r.Sentiment == sentiment);
            }

            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            if (isRead.HasValue)
            {
                query = query.Where(r => r.IsRead == isRead.Value);
            }

            if (isFlagged.HasValue)
            {
                query = query.Where(r => r.IsFlagged == isFlagged.Value);
            }

            var totalCount = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    businessId = r.BusinessId,
                    businessName = r.Business.Name,
                    platform = r.Platform.ToString(),
                    platformReviewId = r.PlatformReviewId,
                    reviewerName = r.ReviewerName,
                    reviewerEmail = r.ReviewerEmail,
                    reviewerAvatarUrl = r.ReviewerAvatarUrl,
                    rating = r.Rating,
                    reviewText = r.ReviewText,
                    reviewDate = r.ReviewDate,
                    responseText = r.ResponseText,
                    responseDate = r.ResponseDate,
                    sentiment = r.Sentiment,
                    sentimentScore = r.SentimentScore,
                    aiSuggestedResponse = r.AiSuggestedResponse,
                    isRead = r.IsRead,
                    isFlagged = r.IsFlagged,
                    location = r.Location
                })
                .ToListAsync();

            return Ok(new
            {
                data = reviews,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews");
            return StatusCode(500, new { message = "Failed to get reviews" });
        }
    }

    /// <summary>
    /// Get a specific review
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id && r.Business.UserId == user.Id);

            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            return Ok(new
            {
                id = review.Id,
                businessId = review.BusinessId,
                businessName = review.Business.Name,
                platform = review.Platform.ToString(),
                reviewerName = review.ReviewerName,
                rating = review.Rating,
                reviewText = review.ReviewText,
                reviewDate = review.ReviewDate,
                responseText = review.ResponseText,
                responseDate = review.ResponseDate,
                sentiment = review.Sentiment,
                isRead = review.IsRead,
                isFlagged = review.IsFlagged
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", id);
            return StatusCode(500, new { message = "Failed to get review" });
        }
    }

    /// <summary>
    /// Reply to a review
    /// </summary>
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> ReplyToReview(int id, [FromBody] ReplyRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id && r.Business.UserId == user.Id);

            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            review.ResponseText = request.ResponseText;
            review.ResponseDate = DateTime.UtcNow;
            review.ResponderId = user.Id;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Replied to review {ReviewId}", id);

            // TODO: Post reply to actual platform API

            return Ok(new
            {
                id = review.Id,
                responseText = review.ResponseText,
                responseDate = review.ResponseDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replying to review {ReviewId}", id);
            return StatusCode(500, new { message = "Failed to reply to review" });
        }
    }

    /// <summary>
    /// Mark review as read/unread
    /// </summary>
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id, [FromBody] MarkReadRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id && r.Business.UserId == user.Id);

            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            review.IsRead = request.IsRead;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { id = review.Id, isRead = review.IsRead });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking review {ReviewId} as read", id);
            return StatusCode(500, new { message = "Failed to update review" });
        }
    }

    /// <summary>
    /// Flag/unflag a review
    /// </summary>
    [HttpPatch("{id}/flag")]
    public async Task<IActionResult> FlagReview(int id, [FromBody] FlagRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id && r.Business.UserId == user.Id);

            if (review == null)
            {
                return NotFound(new { message = "Review not found" });
            }

            review.IsFlagged = request.IsFlagged;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { id = review.Id, isFlagged = review.IsFlagged });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging review {ReviewId}", id);
            return StatusCode(500, new { message = "Failed to update review" });
        }
    }
}

public record ReplyRequest(string ResponseText);
public record MarkReadRequest(bool IsRead);
public record FlagRequest(bool IsFlagged);
