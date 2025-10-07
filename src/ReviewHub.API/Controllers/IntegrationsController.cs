using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using ReviewHub.Infrastructure.Services;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IntegrationsController> _logger;
    private readonly IGoogleBusinessService _googleBusinessService;
    private readonly IYelpService _yelpService;
    private readonly IFacebookService _facebookService;

    public IntegrationsController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<IntegrationsController> logger,
        IGoogleBusinessService googleBusinessService,
        IYelpService yelpService,
        IFacebookService facebookService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _googleBusinessService = googleBusinessService;
        _yelpService = yelpService;
        _facebookService = facebookService;
    }

    /// <summary>
    /// Get all available platforms
    /// </summary>
    [HttpGet("platforms")]
    public IActionResult GetAvailablePlatforms()
    {
        var platforms = Enum.GetValues<ReviewPlatform>()
            .Select(p => new
            {
                id = (int)p,
                name = p.ToString(),
                displayName = GetPlatformDisplayName(p),
                description = GetPlatformDescription(p),
                icon = GetPlatformIcon(p),
                supportsOAuth = SupportsOAuth(p),
                isComingSoon = IsComingSoon(p)
            })
            .ToList();

        return Ok(platforms);
    }

    /// <summary>
    /// Get all platform connections for a business
    /// </summary>
    [HttpGet("business/{businessId}")]
    public async Task<IActionResult> GetBusinessConnections(int businessId)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Verify business belongs to user
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var connections = await _context.PlatformConnections
                .Where(pc => pc.BusinessId == businessId)
                .Select(pc => new
                {
                    id = pc.Id,
                    platform = pc.Platform.ToString(),
                    platformId = (int)pc.Platform,
                    platformBusinessId = pc.PlatformBusinessId,
                    platformBusinessName = pc.PlatformBusinessName,
                    connectedAt = pc.ConnectedAt,
                    lastSyncedAt = pc.LastSyncedAt,
                    isActive = pc.IsActive,
                    autoSync = pc.AutoSync
                })
                .ToListAsync();

            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business connections");
            return StatusCode(500, new { message = "Failed to get connections" });
        }
    }

    /// <summary>
    /// Initiate OAuth flow for a platform
    /// </summary>
    [HttpPost("connect/{platform}")]
    public async Task<IActionResult> InitiateConnection(string platform, [FromBody] ConnectRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Verify business belongs to user
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            if (!Enum.TryParse<ReviewPlatform>(platform, true, out var platformEnum))
            {
                return BadRequest(new { message = "Invalid platform" });
            }

            // Generate auth URL based on platform
            string authUrl;
            switch (platformEnum)
            {
                case ReviewPlatform.Google:
                    authUrl = await _googleBusinessService.GetAuthorizationUrlAsync(user.Id, request.BusinessId);
                    break;

                case ReviewPlatform.Yelp:
                    authUrl = await _yelpService.GetAuthorizationUrlAsync(user.Id, request.BusinessId);
                    break;

                case ReviewPlatform.Facebook:
                    authUrl = await _facebookService.GetAuthorizationUrlAsync(user.Id, request.BusinessId);
                    break;

                default:
                    return BadRequest(new { message = $"{platform} OAuth is not yet implemented" });
            }

            return Ok(new { authUrl, platform = platformEnum.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating platform connection");
            return StatusCode(500, new { message = "Failed to initiate connection" });
        }
    }

    /// <summary>
    /// OAuth callback handler
    /// </summary>
    [HttpGet("callback/{platform}")]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCallback(string platform, [FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (!Enum.TryParse<ReviewPlatform>(platform, true, out var platformEnum))
            {
                return BadRequest(new { message = "Invalid platform" });
            }

            string message;
            switch (platformEnum)
            {
                case ReviewPlatform.Google:
                    message = await _googleBusinessService.ExchangeCodeForTokenAsync(code, state);
                    break;

                case ReviewPlatform.Yelp:
                    message = await _yelpService.ExchangeCodeForTokenAsync(code, state);
                    break;

                case ReviewPlatform.Facebook:
                    message = await _facebookService.ExchangeCodeForTokenAsync(code, state);
                    break;

                default:
                    return BadRequest(new { message = $"{platform} OAuth is not yet implemented" });
            }

            // Redirect to frontend with success message
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            return Redirect($"{frontendUrl}/integrations?success=true&platform={platform}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OAuth callback");
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
            return Redirect($"{frontendUrl}/integrations?error=true&message={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("connect/{platform}/old")]
    public async Task<IActionResult> InitiateConnectionOld(string platform, [FromBody] ConnectRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Verify business belongs to user
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            if (!Enum.TryParse<ReviewPlatform>(platform, true, out var platformEnum))
            {
                return BadRequest(new { message = "Invalid platform" });
            }

            // Check if already connected
            var existingConnection = await _context.PlatformConnections
                .FirstOrDefaultAsync(pc => pc.BusinessId == request.BusinessId && pc.Platform == platformEnum);

            if (existingConnection != null)
            {
                return BadRequest(new { message = "Platform already connected" });
            }

            // Build OAuth URL
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/integrations/callback/{platform}";
            var state = $"{user.Id}:{request.BusinessId}";

            var oauthUrl = platformEnum switch
            {
                ReviewPlatform.Google => BuildGoogleOAuthUrl(callbackUrl, state),
                ReviewPlatform.Yelp => BuildYelpOAuthUrl(callbackUrl, state),
                ReviewPlatform.Facebook => BuildFacebookOAuthUrl(callbackUrl, state),
                _ => null
            };

            if (oauthUrl == null)
            {
                return BadRequest(new { message = "OAuth not available for this platform yet" });
            }

            return Ok(new { authUrl = oauthUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating connection");
            return StatusCode(500, new { message = "Failed to initiate connection" });
        }
    }


    /// <summary>
    /// Disconnect a platform
    /// </summary>
    [HttpDelete("{connectionId}")]
    public async Task<IActionResult> DisconnectPlatform(int connectionId)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var connection = await _context.PlatformConnections
                .Include(pc => pc.Business)
                .FirstOrDefaultAsync(pc => pc.Id == connectionId);

            if (connection == null)
            {
                return NotFound(new { message = "Connection not found" });
            }

            if (connection.Business.UserId != user.Id)
            {
                return Forbid();
            }

            _context.PlatformConnections.Remove(connection);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Disconnected {Platform} for business {BusinessId}", connection.Platform, connection.BusinessId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting platform");
            return StatusCode(500, new { message = "Failed to disconnect platform" });
        }
    }

    /// <summary>
    /// Sync reviews from a platform
    /// </summary>
    [HttpPost("{connectionId}/sync")]
    public async Task<IActionResult> SyncPlatform(int connectionId)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var connection = await _context.PlatformConnections
                .Include(pc => pc.Business)
                .FirstOrDefaultAsync(pc => pc.Id == connectionId);

            if (connection == null)
            {
                return NotFound(new { message = "Connection not found" });
            }

            if (connection.Business.UserId != user.Id)
            {
                return Forbid();
            }

            // Call the appropriate service to fetch reviews
            List<Review> newReviews = new List<Review>();

            switch (connection.Platform)
            {
                case ReviewPlatform.Google:
                    newReviews = await _googleBusinessService.FetchReviewsAsync(connectionId);
                    break;

                case ReviewPlatform.Yelp:
                    newReviews = await _yelpService.FetchReviewsAsync(connectionId);
                    break;

                case ReviewPlatform.Facebook:
                    newReviews = await _facebookService.FetchReviewsAsync(connectionId);
                    break;

                default:
                    return BadRequest(new { message = $"{connection.Platform} sync is not yet implemented" });
            }

            _logger.LogInformation("Synced {Count} new reviews from {Platform} for business {BusinessId}",
                newReviews.Count, connection.Platform, connection.BusinessId);

            return Ok(new {
                message = "Sync completed",
                reviewCount = newReviews.Count,
                lastSyncedAt = connection.LastSyncedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing platform");
            return StatusCode(500, new { message = "Failed to sync platform" });
        }
    }

    // Helper methods
    private static string GetPlatformDisplayName(ReviewPlatform platform) => platform switch
    {
        ReviewPlatform.Google => "Google Business Profile",
        ReviewPlatform.Yelp => "Yelp",
        ReviewPlatform.Facebook => "Facebook",
        ReviewPlatform.TripAdvisor => "TripAdvisor",
        ReviewPlatform.Zomato => "Zomato",
        ReviewPlatform.Trustpilot => "Trustpilot",
        ReviewPlatform.Amazon => "Amazon",
        ReviewPlatform.BookingCom => "Booking.com",
        ReviewPlatform.OpenTable => "OpenTable",
        ReviewPlatform.Foursquare => "Foursquare",
        _ => platform.ToString()
    };

    private static string GetPlatformDescription(ReviewPlatform platform) => platform switch
    {
        ReviewPlatform.Google => "Connect your Google Business Profile to manage reviews and respond to customers",
        ReviewPlatform.Yelp => "Sync Yelp reviews and respond to customer feedback",
        ReviewPlatform.Facebook => "Manage Facebook page reviews and ratings",
        ReviewPlatform.TripAdvisor => "Track and respond to TripAdvisor reviews",
        ReviewPlatform.Zomato => "Connect Zomato restaurant reviews",
        ReviewPlatform.Trustpilot => "Monitor Trustpilot ratings and feedback",
        ReviewPlatform.Amazon => "Manage Amazon product reviews",
        ReviewPlatform.BookingCom => "Track Booking.com property reviews",
        ReviewPlatform.OpenTable => "Monitor OpenTable restaurant reviews",
        ReviewPlatform.Foursquare => "Track Foursquare tips and ratings",
        _ => $"Connect your {platform} account"
    };

    private static string GetPlatformIcon(ReviewPlatform platform) => platform switch
    {
        ReviewPlatform.Google => "google",
        ReviewPlatform.Yelp => "yelp",
        ReviewPlatform.Facebook => "facebook",
        ReviewPlatform.TripAdvisor => "plane",
        ReviewPlatform.Zomato => "utensils",
        ReviewPlatform.Trustpilot => "shield-check",
        ReviewPlatform.Amazon => "shopping-cart",
        ReviewPlatform.BookingCom => "hotel",
        ReviewPlatform.OpenTable => "calendar",
        ReviewPlatform.Foursquare => "map-pin",
        _ => "link"
    };

    private static bool SupportsOAuth(ReviewPlatform platform) => platform switch
    {
        ReviewPlatform.Google => true,
        ReviewPlatform.Yelp => true,
        ReviewPlatform.Facebook => true,
        _ => false
    };

    private static bool IsComingSoon(ReviewPlatform platform) => platform switch
    {
        ReviewPlatform.Google => false,
        ReviewPlatform.Yelp => false,
        ReviewPlatform.Facebook => false,
        _ => true
    };

    private string BuildGoogleOAuthUrl(string callbackUrl, string state)
    {
        var clientId = _configuration["Google:ClientId"];
        var scopes = Uri.EscapeDataString("https://www.googleapis.com/auth/business.manage");

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={clientId}&" +
               $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
               $"response_type=code&" +
               $"scope={scopes}&" +
               $"state={state}&" +
               $"access_type=offline&" +
               $"prompt=consent";
    }

    private string BuildYelpOAuthUrl(string callbackUrl, string state)
    {
        var clientId = _configuration["Yelp:ClientId"];
        return $"https://www.yelp.com/oauth2/v2/authorize?" +
               $"client_id={clientId}&" +
               $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
               $"state={state}";
    }

    private string BuildFacebookOAuthUrl(string callbackUrl, string state)
    {
        var appId = _configuration["Facebook:AppId"];
        var scopes = Uri.EscapeDataString("pages_manage_metadata,pages_read_engagement,pages_read_user_content");

        return $"https://www.facebook.com/v18.0/dialog/oauth?" +
               $"client_id={appId}&" +
               $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
               $"state={state}&" +
               $"scope={scopes}";
    }

    private Task<OAuthTokenResponse?> ExchangeCodeForTokens(ReviewPlatform platform, string code)
    {
        // TODO: Implement real OAuth token exchange
        // This is a mock implementation
        var response = new OAuthTokenResponse
        {
            AccessToken = $"mock_token_{platform}_{Guid.NewGuid()}",
            RefreshToken = $"mock_refresh_{platform}_{Guid.NewGuid()}",
            ExpiresIn = 3600,
            PlatformBusinessId = $"{platform}_business_123",
            PlatformBusinessName = $"Demo {platform} Business"
        };

        return Task.FromResult<OAuthTokenResponse?>(response);
    }
}

public record ConnectRequest(int BusinessId);

public class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? PlatformBusinessId { get; set; }
    public string? PlatformBusinessName { get; set; }
}
