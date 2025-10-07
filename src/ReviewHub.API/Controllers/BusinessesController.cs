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
public class BusinessesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BusinessesController> _logger;

    public BusinessesController(ApplicationDbContext context, ILogger<BusinessesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all businesses for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBusinesses()
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var businesses = await _context.Businesses
                .Where(b => b.UserId == user.Id && b.IsActive)
                .Include(b => b.PlatformConnections)
                .Include(b => b.Reviews)
                .Select(b => new
                {
                    id = b.Id,
                    name = b.Name,
                    industry = b.Industry,
                    description = b.Description,
                    website = b.Website,
                    phoneNumber = b.PhoneNumber,
                    address = b.Address,
                    city = b.City,
                    state = b.State,
                    zipCode = b.ZipCode,
                    country = b.Country,
                    logoUrl = b.LogoUrl,
                    createdAt = b.CreatedAt,
                    platformConnectionsCount = b.PlatformConnections.Count(pc => pc.IsActive),
                    reviewsCount = b.Reviews.Count(),
                    avgRating = b.Reviews.Any() ? b.Reviews.Average(r => (double)r.Rating) : 0
                })
                .OrderByDescending(b => b.createdAt)
                .ToListAsync();

            return Ok(businesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting businesses");
            return StatusCode(500, new { message = "Failed to get businesses" });
        }
    }

    /// <summary>
    /// Get a specific business by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBusiness(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var business = await _context.Businesses
                .Include(b => b.PlatformConnections)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var result = new
            {
                id = business.Id,
                name = business.Name,
                industry = business.Industry,
                description = business.Description,
                website = business.Website,
                phoneNumber = business.PhoneNumber,
                address = business.Address,
                city = business.City,
                state = business.State,
                zipCode = business.ZipCode,
                country = business.Country,
                logoUrl = business.LogoUrl,
                createdAt = business.CreatedAt,
                platformConnections = business.PlatformConnections
                    .Where(pc => pc.IsActive)
                    .Select(pc => new
                    {
                        id = pc.Id,
                        platform = pc.Platform.ToString(),
                        connectedAt = pc.ConnectedAt,
                        lastSyncedAt = pc.LastSyncedAt
                    }),
                stats = new
                {
                    totalReviews = business.Reviews.Count(),
                    avgRating = business.Reviews.Any() ? business.Reviews.Average(r => (double)r.Rating) : 0,
                    unreadReviews = business.Reviews.Count(r => !r.IsRead),
                    flaggedReviews = business.Reviews.Count(r => r.IsFlagged)
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business {BusinessId}", id);
            return StatusCode(500, new { message = "Failed to get business" });
        }
    }

    /// <summary>
    /// Create a new business
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var business = new Business
            {
                UserId = user.Id,
                Name = request.Name,
                Industry = request.Industry,
                Description = request.Description,
                Website = request.Website,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country,
                LogoUrl = request.LogoUrl,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created business {BusinessId} for user {UserId}", business.Id, user.Id);

            return CreatedAtAction(nameof(GetBusiness), new { id = business.Id }, new
            {
                id = business.Id,
                name = business.Name,
                industry = business.Industry,
                createdAt = business.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business");
            return StatusCode(500, new { message = "Failed to create business" });
        }
    }

    /// <summary>
    /// Update a business
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBusiness(int id, [FromBody] UpdateBusinessRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            business.Name = request.Name ?? business.Name;
            business.Industry = request.Industry ?? business.Industry;
            business.Description = request.Description ?? business.Description;
            business.Website = request.Website ?? business.Website;
            business.PhoneNumber = request.PhoneNumber ?? business.PhoneNumber;
            business.Address = request.Address ?? business.Address;
            business.City = request.City ?? business.City;
            business.State = request.State ?? business.State;
            business.ZipCode = request.ZipCode ?? business.ZipCode;
            business.Country = request.Country ?? business.Country;
            business.LogoUrl = request.LogoUrl ?? business.LogoUrl;
            business.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated business {BusinessId}", id);

            return Ok(new
            {
                id = business.Id,
                name = business.Name,
                industry = business.Industry,
                updatedAt = business.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business {BusinessId}", id);
            return StatusCode(500, new { message = "Failed to update business" });
        }
    }

    /// <summary>
    /// Delete (deactivate) a business
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBusiness(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Soft delete
            business.IsActive = false;
            business.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted business {BusinessId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business {BusinessId}", id);
            return StatusCode(500, new { message = "Failed to delete business" });
        }
    }
}

public record CreateBusinessRequest(
    string Name,
    string? Industry,
    string? Description,
    string? Website,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    string? LogoUrl
);

public record UpdateBusinessRequest(
    string? Name,
    string? Industry,
    string? Description,
    string? Website,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    string? LogoUrl
);
