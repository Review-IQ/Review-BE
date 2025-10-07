using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReviewHub.Infrastructure.Services;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompetitorController : ControllerBase
{
    private readonly ICompetitorService _competitorService;
    private readonly ILogger<CompetitorController> _logger;

    public CompetitorController(
        ICompetitorService competitorService,
        ILogger<CompetitorController> logger)
    {
        _competitorService = competitorService;
        _logger = logger;
    }

    /// <summary>
    /// Search for potential competitors using Google Places API
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchCompetitors([FromQuery] string businessName, [FromQuery] string location)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(location))
            {
                return BadRequest(new { message = "Business name and location are required" });
            }

            var results = await _competitorService.SearchCompetitorsAsync(businessName, location);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching competitors");
            return StatusCode(500, new { message = "Error searching competitors" });
        }
    }

    /// <summary>
    /// Get detailed information about a competitor from Google Places
    /// </summary>
    [HttpGet("details/{placeId}")]
    public async Task<IActionResult> GetCompetitorDetails(string placeId)
    {
        try
        {
            var details = await _competitorService.GetCompetitorDetailsAsync(placeId);
            if (details == null)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor details for {PlaceId}", placeId);
            return StatusCode(500, new { message = "Error getting competitor details" });
        }
    }

    /// <summary>
    /// Get reviews for a competitor from Google Places
    /// </summary>
    [HttpGet("reviews/{placeId}")]
    public async Task<IActionResult> GetCompetitorReviews(string placeId)
    {
        try
        {
            var reviews = await _competitorService.GetCompetitorReviewsAsync(placeId);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor reviews for {PlaceId}", placeId);
            return StatusCode(500, new { message = "Error getting competitor reviews" });
        }
    }

    /// <summary>
    /// Add a competitor to track
    /// </summary>
    [HttpPost("{businessId}/add")]
    public async Task<IActionResult> AddCompetitor(int businessId, [FromBody] AddCompetitorRequest request)
    {
        try
        {
            var competitor = await _competitorService.AddCompetitorAsync(businessId, request.PlaceId, request.Name);
            return Ok(competitor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding competitor");
            return StatusCode(500, new { message = "Error adding competitor" });
        }
    }

    /// <summary>
    /// Get all tracked competitors for a business
    /// </summary>
    [HttpGet("{businessId}")]
    public async Task<IActionResult> GetCompetitors(int businessId)
    {
        try
        {
            var competitors = await _competitorService.GetCompetitorsAsync(businessId);
            return Ok(competitors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitors for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "Error getting competitors" });
        }
    }

    /// <summary>
    /// Refresh competitor data from Google Places
    /// </summary>
    [HttpPost("{businessId}/refresh/{competitorId}")]
    public async Task<IActionResult> RefreshCompetitor(int businessId, int competitorId)
    {
        try
        {
            var competitor = await _competitorService.RefreshCompetitorDataAsync(competitorId);
            return Ok(competitor);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing competitor {CompetitorId}", competitorId);
            return StatusCode(500, new { message = "Error refreshing competitor data" });
        }
    }

    /// <summary>
    /// Remove a tracked competitor
    /// </summary>
    [HttpDelete("{businessId}/{competitorId}")]
    public async Task<IActionResult> RemoveCompetitor(int businessId, int competitorId)
    {
        try
        {
            var success = await _competitorService.RemoveCompetitorAsync(competitorId, businessId);
            if (!success)
            {
                return NotFound(new { message = "Competitor not found" });
            }

            return Ok(new { message = "Competitor removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing competitor {CompetitorId}", competitorId);
            return StatusCode(500, new { message = "Error removing competitor" });
        }
    }
}

public record AddCompetitorRequest(string PlaceId, string Name);
