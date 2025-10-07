using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class YelpService : IYelpService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YelpService> _logger;

    private const string YELP_AUTH_URL = "https://www.yelp.com/oauth2/v2/authorize";
    private const string YELP_TOKEN_URL = "https://api.yelp.com/oauth2/v2/token";
    private const string YELP_API_BASE = "https://api.yelp.com/v3";

    public YelpService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<YelpService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAuthorizationUrlAsync(int userId, int businessId)
    {
        var clientId = _configuration["Yelp:ClientId"];
        var redirectUri = _configuration["Yelp:RedirectUri"];
        var state = $"{userId}:{businessId}:{Guid.NewGuid()}";

        var authUrl = $"{YELP_AUTH_URL}?" +
            $"client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&state={Uri.EscapeDataString(state)}";

        return authUrl;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string state)
    {
        var parts = state.Split(':');
        if (parts.Length < 2) throw new ArgumentException("Invalid state parameter");

        var userId = int.Parse(parts[0]);
        var businessId = int.Parse(parts[1]);

        var clientId = _configuration["Yelp:ClientId"];
        var clientSecret = _configuration["Yelp:ClientSecret"];
        var redirectUri = _configuration["Yelp:RedirectUri"];

        var client = _httpClientFactory.CreateClient();

        var requestBody = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        var response = await client.PostAsync(
            YELP_TOKEN_URL,
            new FormUrlEncodedContent(requestBody)
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Yelp token exchange failed: {Error}", error);
            throw new Exception($"Token exchange failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<YelpTokenResponse>(json);

        // Save to database
        var connection = await _context.PlatformConnections
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Platform == ReviewPlatform.Yelp);

        if (connection == null)
        {
            connection = new PlatformConnection
            {
                BusinessId = businessId,
                Platform = ReviewPlatform.Yelp,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            };
            _context.PlatformConnections.Add(connection);
        }

        connection.AccessToken = tokenData.access_token;
        connection.RefreshToken = tokenData.refresh_token;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);
        connection.LastSyncedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Yelp OAuth token saved for business {BusinessId}", businessId);

        return "Yelp connected successfully";
    }

    public async Task<List<Review>> FetchReviewsAsync(int platformConnectionId)
    {
        var connection = await _context.PlatformConnections
            .Include(c => c.Business)
            .FirstOrDefaultAsync(c => c.Id == platformConnectionId);

        if (connection == null)
            throw new Exception("Platform connection not found");

        // Check if token needs refresh
        if (connection.TokenExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            await RefreshAccessTokenAsync(platformConnectionId);
            connection = await _context.PlatformConnections.FindAsync(platformConnectionId);
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

        var reviews = new List<Review>();

        try
        {
            // Get business ID from Yelp
            var businessAlias = connection.PlatformBusinessId;

            if (string.IsNullOrEmpty(businessAlias))
            {
                _logger.LogWarning("No Yelp business ID configured for connection {ConnectionId}", platformConnectionId);
                return reviews;
            }

            // Fetch reviews from Yelp
            var reviewsResponse = await client.GetAsync($"{YELP_API_BASE}/businesses/{businessAlias}/reviews");

            if (!reviewsResponse.IsSuccessStatusCode)
            {
                var error = await reviewsResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch Yelp reviews: {Error}", error);
                throw new Exception($"Failed to fetch reviews: {error}");
            }

            var reviewsJson = await reviewsResponse.Content.ReadAsStringAsync();
            var reviewsData = JsonSerializer.Deserialize<YelpReviewsResponse>(reviewsJson);

            if (reviewsData?.reviews == null) return reviews;

            foreach (var yelpReview in reviewsData.reviews)
            {
                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.PlatformReviewId == yelpReview.id);

                if (existingReview != null) continue;

                var review = new Review
                {
                    BusinessId = connection.BusinessId,
                    Platform = ReviewPlatform.Yelp,
                    PlatformReviewId = yelpReview.id,
                    ReviewerName = yelpReview.user?.name ?? "Anonymous",
                    Rating = yelpReview.rating,
                    ReviewText = yelpReview.text,
                    ReviewDate = DateTime.Parse(yelpReview.time_created),
                    Sentiment = DetermineSentiment(yelpReview.rating),
                    IsRead = false,
                    IsFlagged = false,
                    CreatedAt = DateTime.UtcNow
                };

                reviews.Add(review);
            }

            // Save all new reviews
            if (reviews.Any())
            {
                _context.Reviews.AddRange(reviews);
                connection.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Fetched {Count} new Yelp reviews for business {BusinessId}",
                    reviews.Count, connection.BusinessId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Yelp reviews");
            throw;
        }

        return reviews;
    }

    public async Task RefreshAccessTokenAsync(int platformConnectionId)
    {
        var connection = await _context.PlatformConnections.FindAsync(platformConnectionId);
        if (connection == null)
            throw new Exception("Platform connection not found");

        var clientId = _configuration["Yelp:ClientId"];
        var clientSecret = _configuration["Yelp:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var requestBody = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "refresh_token", connection.RefreshToken },
            { "grant_type", "refresh_token" }
        };

        var response = await client.PostAsync(
            YELP_TOKEN_URL,
            new FormUrlEncodedContent(requestBody)
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Yelp token refresh failed: {Error}", error);
            throw new Exception($"Token refresh failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<YelpTokenResponse>(json);

        connection.AccessToken = tokenData.access_token;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Yelp access token refreshed for connection {ConnectionId}", platformConnectionId);
    }

    private string DetermineSentiment(int rating)
    {
        return rating switch
        {
            5 or 4 => "Positive",
            3 => "Neutral",
            2 or 1 => "Negative",
            _ => "Neutral"
        };
    }
}

// DTOs for Yelp API responses
public class YelpTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public int expires_in { get; set; }
    public string token_type { get; set; } = string.Empty;
}

public class YelpReviewsResponse
{
    public YelpReview[]? reviews { get; set; }
    public int total { get; set; }
}

public class YelpReview
{
    public string id { get; set; } = string.Empty;
    public int rating { get; set; }
    public YelpUser? user { get; set; }
    public string text { get; set; } = string.Empty;
    public string time_created { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
}

public class YelpUser
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string image_url { get; set; } = string.Empty;
}
