using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using ReviewHub.Infrastructure.Services;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ApplicationDbContext context,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = _context.Users.FirstOrDefault(u => u.Auth0Id == auth0Id);
        return user?.Id ?? 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations([FromQuery] int? organizationId)
    {
        try
        {
            var userId = GetUserId();
            var locations = await _locationService.GetUserAccessibleLocationsAsync(userId);

            return Ok(locations.Select(l => new
            {
                l.Id,
                l.Name,
                l.Address,
                l.City,
                l.State,
                l.ZipCode,
                l.Country,
                l.PhoneNumber,
                l.Email,
                l.Latitude,
                l.Longitude,
                l.IsActive,
                LocationGroup = l.LocationGroup != null ? new
                {
                    l.LocationGroup.Id,
                    l.LocationGroup.Name,
                    l.LocationGroup.GroupType
                } : null,
                Manager = l.Manager != null ? new
                {
                    l.Manager.Id,
                    l.Manager.FullName,
                    l.Manager.Email
                } : null
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations");
            return StatusCode(500, new { message = "Error retrieving locations" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLocation(int id)
    {
        try
        {
            var userId = GetUserId();
            var hasAccess = await _locationService.UserHasAccessToLocationAsync(userId, id);

            if (!hasAccess)
                return Forbid();

            var location = await _context.Locations
                .Include(l => l.LocationGroup)
                .Include(l => l.Manager)
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
                return NotFound();

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location {LocationId}", id);
            return StatusCode(500, new { message = "Error retrieving location" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        try
        {
            var location = await _locationService.CreateLocationAsync(
                request.OrganizationId,
                request.Name,
                request.Address ?? "",
                request.City ?? "",
                request.State ?? "",
                request.ZipCode ?? "",
                request.LocationGroupId
            );

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return StatusCode(500, new { message = "Error creating location" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
    {
        try
        {
            var userId = GetUserId();
            var hasAccess = await _locationService.UserHasAccessToLocationAsync(userId, id);

            if (!hasAccess)
                return Forbid();

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();

            location.Name = request.Name ?? location.Name;
            location.Address = request.Address ?? location.Address;
            location.City = request.City ?? location.City;
            location.State = request.State ?? location.State;
            location.ZipCode = request.ZipCode ?? location.ZipCode;
            location.PhoneNumber = request.PhoneNumber ?? location.PhoneNumber;
            location.Email = request.Email ?? location.Email;
            location.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {LocationId}", id);
            return StatusCode(500, new { message = "Error updating location" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        try
        {
            var userId = GetUserId();
            var hasAccess = await _locationService.UserHasAccessToLocationAsync(userId, id);

            if (!hasAccess)
                return Forbid();

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return NotFound();

            // Soft delete by marking as inactive
            location.IsActive = false;
            location.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Location deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {LocationId}", id);
            return StatusCode(500, new { message = "Error deleting location" });
        }
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetLocationGroups([FromQuery] int? organizationId)
    {
        try
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user?.OrganizationId == null)
                return BadRequest(new { message = "User not assigned to organization" });

            var orgId = organizationId ?? user.OrganizationId.Value;

            var groups = await _context.LocationGroups
                .Where(lg => lg.OrganizationId == orgId && lg.IsActive)
                .Include(lg => lg.ParentGroup)
                .Include(lg => lg.ChildGroups)
                .Include(lg => lg.Locations)
                .OrderBy(lg => lg.Level)
                .ThenBy(lg => lg.Name)
                .ToListAsync();

            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location groups");
            return StatusCode(500, new { message = "Error retrieving location groups" });
        }
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateLocationGroup([FromBody] CreateLocationGroupRequest request)
    {
        try
        {
            var group = await _locationService.CreateLocationGroupAsync(
                request.OrganizationId,
                request.Name,
                request.GroupType,
                request.ParentGroupId
            );

            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location group");
            return StatusCode(500, new { message = "Error creating location group" });
        }
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareLocations([FromQuery] string locationIds)
    {
        try
        {
            var ids = locationIds.Split(',').Select(int.Parse).ToList();
            var userId = GetUserId();

            // Verify user has access to all requested locations
            foreach (var id in ids)
            {
                if (!await _locationService.UserHasAccessToLocationAsync(userId, id))
                    return Forbid();
            }

            // Get comparison data
            var comparisonData = await _context.Reviews
                .Where(r => r.LocationId.HasValue && ids.Contains(r.LocationId.Value))
                .GroupBy(r => r.LocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    TotalReviews = g.Count(),
                    AverageRating = g.Average(r => r.Rating),
                    PositiveSentiment = g.Count(r => r.Sentiment == "Positive") * 100.0 / g.Count(),
                    NeutralSentiment = g.Count(r => r.Sentiment == "Neutral") * 100.0 / g.Count(),
                    NegativeSentiment = g.Count(r => r.Sentiment == "Negative") * 100.0 / g.Count(),
                    ResponseRate = g.Count(r => r.ResponseText != null) * 100.0 / g.Count(),
                    RecentReviews = g.Count(r => r.ReviewDate >= DateTime.UtcNow.AddDays(-30))
                })
                .ToListAsync();

            // Get location names
            var locations = await _context.Locations
                .Where(l => ids.Contains(l.Id))
                .Select(l => new { l.Id, l.Name })
                .ToListAsync();

            var result = comparisonData.Select(data =>
            {
                var location = locations.FirstOrDefault(l => l.Id == data.LocationId);
                return new
                {
                    data.LocationId,
                    LocationName = location?.Name ?? "Unknown",
                    data.TotalReviews,
                    data.AverageRating,
                    data.PositiveSentiment,
                    data.NeutralSentiment,
                    data.NegativeSentiment,
                    data.ResponseRate,
                    data.RecentReviews
                };
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing locations");
            return StatusCode(500, new { message = "Error comparing locations" });
        }
    }

    [HttpPost("access/user/{userId}/locations")]
    public async Task<IActionResult> AssignUserToLocations(int userId, [FromBody] List<int> locationIds)
    {
        try
        {
            await _locationService.AssignUserToLocationsAsync(userId, locationIds);
            return Ok(new { message = "User assigned to locations successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user to locations");
            return StatusCode(500, new { message = "Error assigning user to locations" });
        }
    }

    [HttpPost("access/user/{userId}/group/{groupId}")]
    public async Task<IActionResult> AssignUserToLocationGroup(int userId, int groupId)
    {
        try
        {
            await _locationService.AssignUserToLocationGroupAsync(userId, groupId);
            return Ok(new { message = "User assigned to location group successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user to location group");
            return StatusCode(500, new { message = "Error assigning user to location group" });
        }
    }

    [HttpPost("access/user/{userId}/all")]
    public async Task<IActionResult> AssignUserToAllLocations(int userId, [FromQuery] int organizationId)
    {
        try
        {
            await _locationService.AssignUserToAllLocationsAsync(userId, organizationId);
            return Ok(new { message = "User assigned to all locations successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user to all locations");
            return StatusCode(500, new { message = "Error assigning user to all locations" });
        }
    }
}

public record CreateLocationRequest(
    int OrganizationId,
    string Name,
    string? Address,
    string? City,
    string? State,
    string? ZipCode,
    int? LocationGroupId
);

public record UpdateLocationRequest(
    string? Name,
    string? Address,
    string? City,
    string? State,
    string? ZipCode,
    string? PhoneNumber,
    string? Email
);

public record CreateLocationGroupRequest(
    int OrganizationId,
    string Name,
    string? GroupType,
    int? ParentGroupId
);
