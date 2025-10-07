using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompetitorsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompetitorsController> _logger;

    public CompetitorsController(
        ApplicationDbContext context,
        ILogger<CompetitorsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{businessId}")]
    public async Task<IActionResult> GetCompetitors(int businessId)
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

            var competitors = await _context.Competitors
                .Where(c => c.BusinessId == businessId)
                .OrderByDescending(c => c.CurrentRating)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Platform,
                    PlatformBusinessId = c.PlatformBusinessId,
                    CurrentRating = c.CurrentRating,
                    TotalReviews = c.TotalReviews,
                    LastSyncedAt = c.LastCheckedAt,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(new { competitors = competitors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitors");
            return StatusCode(500, new { message = "Failed to get competitors" });
        }
    }

    [HttpGet("detail/{id}")]
    public async Task<IActionResult> GetCompetitor(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var competitor = await _context.Competitors
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (competitor == null)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            return Ok(new
            {
                competitor.Id,
                competitor.Name,
                competitor.Platform,
                PlatformBusinessId = competitor.PlatformBusinessId,
                CurrentRating = competitor.CurrentRating,
                TotalReviews = competitor.TotalReviews,
                LastSyncedAt = competitor.LastCheckedAt,
                competitor.CreatedAt,
                BusinessName = competitor.Business.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor");
            return StatusCode(500, new { message = "Failed to get competitor" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompetitor([FromBody] CreateCompetitorRequest request)
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
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Check if competitor already exists
            var existingCompetitor = await _context.Competitors
                .FirstOrDefaultAsync(c => c.BusinessId == request.BusinessId &&
                                         c.Platform.ToString() == request.Platform &&
                                         c.PlatformBusinessId == request.PlatformBusinessId);

            if (existingCompetitor != null)
            {
                return BadRequest(new { message = "This competitor is already being tracked" });
            }

            var competitor = new Competitor
            {
                BusinessId = request.BusinessId,
                Name = request.Name,
                Platform = Enum.Parse<ReviewHub.Core.Enums.ReviewPlatform>(request.Platform),
                PlatformBusinessId = request.PlatformBusinessId,
                CurrentRating = null,
                TotalReviews = null,
                LastCheckedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Competitors.Add(competitor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompetitor), new { id = competitor.Id }, new
            {
                competitor.Id,
                competitor.Name,
                competitor.Platform,
                PlatformBusinessId = competitor.PlatformBusinessId,
                CurrentRating = competitor.CurrentRating,
                TotalReviews = competitor.TotalReviews,
                competitor.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating competitor");
            return StatusCode(500, new { message = "Failed to create competitor" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompetitor(int id, [FromBody] UpdateCompetitorRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var competitor = await _context.Competitors
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (competitor == null)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            competitor.Name = request.Name;
            competitor.PlatformBusinessId = request.PlatformBusinessId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                competitor.Id,
                competitor.Name,
                competitor.Platform,
                PlatformBusinessId = competitor.PlatformBusinessId,
                CurrentRating = competitor.CurrentRating,
                TotalReviews = competitor.TotalReviews
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating competitor");
            return StatusCode(500, new { message = "Failed to update competitor" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompetitor(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var competitor = await _context.Competitors
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (competitor == null)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            _context.Competitors.Remove(competitor);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting competitor");
            return StatusCode(500, new { message = "Failed to delete competitor" });
        }
    }

    [HttpPost("{id}/sync")]
    public async Task<IActionResult> SyncCompetitor(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var competitor = await _context.Competitors
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (competitor == null)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            // TODO: Implement actual platform API integration to fetch competitor data
            // For now, we'll simulate the sync with mock data
            var random = new Random();
            competitor.CurrentRating = Math.Round(3.5 + random.NextDouble() * 1.5, 1); // 3.5-5.0
            competitor.TotalReviews = random.Next(50, 500);
            competitor.LastCheckedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Synced competitor {CompetitorId} data", id);

            return Ok(new
            {
                competitor.Id,
                competitor.Name,
                competitor.Platform,
                CurrentRating = competitor.CurrentRating,
                TotalReviews = competitor.TotalReviews,
                LastSyncedAt = competitor.LastCheckedAt,
                message = "Competitor data synced successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing competitor");
            return StatusCode(500, new { message = "Failed to sync competitor" });
        }
    }

    [HttpGet("comparison/{businessId}")]
    public async Task<IActionResult> GetComparisonData(int businessId)
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

            // Get business average rating
            var businessReviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .ToListAsync();

            var businessAverageRating = businessReviews.Any()
                ? Math.Round(businessReviews.Average(r => r.Rating), 1)
                : 0;

            // Get competitors data
            var competitors = await _context.Competitors
                .Where(c => c.BusinessId == businessId)
                .Select(c => new
                {
                    c.Name,
                    c.Platform,
                    CurrentRating = c.CurrentRating,
                    TotalReviews = c.TotalReviews
                })
                .ToListAsync();

            // Calculate industry average (business + all competitors with ratings)
            var allRatings = new List<double> { businessAverageRating };
            allRatings.AddRange(competitors.Where(c => c.CurrentRating.HasValue).Select(c => c.CurrentRating!.Value));
            var industryAverage = allRatings.Any() ? Math.Round(allRatings.Average(), 1) : 0;

            return Ok(new
            {
                business = new
                {
                    Name = business.Name,
                    AverageRating = businessAverageRating,
                    TotalReviews = businessReviews.Count
                },
                competitors = competitors,
                industryAverage = industryAverage,
                performanceVsIndustry = businessAverageRating - industryAverage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comparison data");
            return StatusCode(500, new { message = "Failed to get comparison data" });
        }
    }
}

public record CreateCompetitorRequest(
    int BusinessId,
    string Name,
    string Platform,
    string PlatformBusinessId);

public record UpdateCompetitorRequest(
    string Name,
    string PlatformBusinessId);
