using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class GoogleBusinessService : IGoogleBusinessService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleBusinessService> _logger;

    private const string GOOGLE_AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GOOGLE_TOKEN_URL = "https://oauth2.googleapis.com/token";
    private const string GOOGLE_API_BASE = "https://mybusiness.googleapis.com/v4";

    public GoogleBusinessService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleBusinessService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAuthorizationUrlAsync(int userId, int businessId)
    {
        var clientId = _configuration["Google:ClientId"];
        var redirectUri = _configuration["Google:RedirectUri"];
        var state = $"{userId}:{businessId}:{Guid.NewGuid()}";

        var scopes = new[]
        {
            "https://www.googleapis.com/auth/business.manage",
            "https://www.googleapis.com/auth/plus.business.manage"
        };

        var authUrl = $"{GOOGLE_AUTH_URL}?" +
            $"client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(string.Join(" ", scopes))}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&access_type=offline" +
            $"&prompt=consent";

        return authUrl;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string state)
    {
        var parts = state.Split(':');
        if (parts.Length < 2) throw new ArgumentException("Invalid state parameter");

        var userId = int.Parse(parts[0]);
        var businessId = int.Parse(parts[1]);

        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];
        var redirectUri = _configuration["Google:RedirectUri"];

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
            GOOGLE_TOKEN_URL,
            new FormUrlEncodedContent(requestBody)
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google token exchange failed: {Error}", error);
            throw new Exception($"Token exchange failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(json);

        // Save to database
        var connection = await _context.PlatformConnections
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Platform == ReviewPlatform.Google);

        if (connection == null)
        {
            connection = new PlatformConnection
            {
                BusinessId = businessId,
                Platform = ReviewPlatform.Google,
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

        _logger.LogInformation("Google OAuth token saved for business {BusinessId}", businessId);

        return "Google Business Profile connected successfully";
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
            // First, get the account
            var accountsResponse = await client.GetAsync($"{GOOGLE_API_BASE}/accounts");
            if (!accountsResponse.IsSuccessStatusCode)
            {
                var error = await accountsResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch Google accounts: {Error}", error);
                throw new Exception($"Failed to fetch accounts: {error}");
            }

            var accountsJson = await accountsResponse.Content.ReadAsStringAsync();
            var accountsData = JsonSerializer.Deserialize<GoogleAccountsResponse>(accountsJson);

            if (accountsData?.accounts == null || accountsData.accounts.Length == 0)
            {
                _logger.LogWarning("No Google Business accounts found");
                return reviews;
            }

            // Get the first account (can be enhanced to let user select)
            var accountName = accountsData.accounts[0].name;

            // Get locations for this account
            var locationsResponse = await client.GetAsync($"{GOOGLE_API_BASE}/{accountName}/locations");
            if (!locationsResponse.IsSuccessStatusCode)
            {
                var error = await locationsResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch locations: {Error}", error);
                throw new Exception($"Failed to fetch locations: {error}");
            }

            var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
            var locationsData = JsonSerializer.Deserialize<GoogleLocationsResponse>(locationsJson);

            if (locationsData?.locations == null || locationsData.locations.Length == 0)
            {
                _logger.LogWarning("No Google Business locations found");
                return reviews;
            }

            // Get reviews for each location
            foreach (var location in locationsData.locations)
            {
                var reviewsResponse = await client.GetAsync($"{GOOGLE_API_BASE}/{location.name}/reviews");

                if (!reviewsResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch reviews for location {Location}", location.name);
                    continue;
                }

                var reviewsJson = await reviewsResponse.Content.ReadAsStringAsync();
                var reviewsData = JsonSerializer.Deserialize<GoogleReviewsResponse>(reviewsJson);

                if (reviewsData?.reviews == null) continue;

                foreach (var googleReview in reviewsData.reviews)
                {
                    // Check if review already exists
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.PlatformReviewId == googleReview.reviewId);

                    if (existingReview != null) continue;

                    var review = new Review
                    {
                        BusinessId = connection.BusinessId,
                        Platform = ReviewPlatform.Google,
                        PlatformReviewId = googleReview.reviewId,
                        ReviewerName = googleReview.reviewer?.displayName ?? "Anonymous",
                        Rating = googleReview.starRating switch
                        {
                            "FIVE" => 5,
                            "FOUR" => 4,
                            "THREE" => 3,
                            "TWO" => 2,
                            "ONE" => 1,
                            _ => 0
                        },
                        ReviewText = googleReview.comment,
                        ReviewDate = DateTime.Parse(googleReview.createTime),
                        Sentiment = DetermineSentiment(googleReview.starRating),
                        IsRead = false,
                        IsFlagged = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    reviews.Add(review);
                }
            }

            // Save all new reviews
            if (reviews.Any())
            {
                _context.Reviews.AddRange(reviews);
                connection.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Fetched {Count} new Google reviews for business {BusinessId}",
                    reviews.Count, connection.BusinessId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google reviews");
            throw;
        }

        return reviews;
    }

    public async Task RefreshAccessTokenAsync(int platformConnectionId)
    {
        var connection = await _context.PlatformConnections.FindAsync(platformConnectionId);
        if (connection == null)
            throw new Exception("Platform connection not found");

        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var requestBody = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "refresh_token", connection.RefreshToken },
            { "grant_type", "refresh_token" }
        };

        var response = await client.PostAsync(
            GOOGLE_TOKEN_URL,
            new FormUrlEncodedContent(requestBody)
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google token refresh failed: {Error}", error);
            throw new Exception($"Token refresh failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(json);

        connection.AccessToken = tokenData.access_token;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Google access token refreshed for connection {ConnectionId}", platformConnectionId);
    }

    private string DetermineSentiment(string starRating)
    {
        return starRating switch
        {
            "FIVE" or "FOUR" => "Positive",
            "THREE" => "Neutral",
            "TWO" or "ONE" => "Negative",
            _ => "Neutral"
        };
    }
}

// DTOs for Google API responses
public class GoogleTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public int expires_in { get; set; }
    public string token_type { get; set; } = string.Empty;
}

public class GoogleAccountsResponse
{
    public GoogleAccount[]? accounts { get; set; }
}

public class GoogleAccount
{
    public string name { get; set; } = string.Empty;
    public string accountName { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
}

public class GoogleLocationsResponse
{
    public GoogleLocation[]? locations { get; set; }
}

public class GoogleLocation
{
    public string name { get; set; } = string.Empty;
    public string locationName { get; set; } = string.Empty;
}

public class GoogleReviewsResponse
{
    public GoogleReview[]? reviews { get; set; }
}

public class GoogleReview
{
    public string reviewId { get; set; } = string.Empty;
    public GoogleReviewer? reviewer { get; set; }
    public string starRating { get; set; } = string.Empty;
    public string comment { get; set; } = string.Empty;
    public string createTime { get; set; } = string.Empty;
    public string updateTime { get; set; } = string.Empty;
}

public class GoogleReviewer
{
    public string displayName { get; set; } = string.Empty;
    public string profilePhotoUrl { get; set; } = string.Empty;
}
