using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class FacebookService : IFacebookService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FacebookService> _logger;

    private const string FB_AUTH_URL = "https://www.facebook.com/v18.0/dialog/oauth";
    private const string FB_TOKEN_URL = "https://graph.facebook.com/v18.0/oauth/access_token";
    private const string FB_API_BASE = "https://graph.facebook.com/v18.0";

    public FacebookService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<FacebookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAuthorizationUrlAsync(int userId, int businessId)
    {
        var appId = _configuration["Facebook:AppId"];
        var redirectUri = _configuration["Facebook:RedirectUri"];
        var state = $"{userId}:{businessId}:{Guid.NewGuid()}";

        var scopes = new[]
        {
            "pages_show_list",
            "pages_read_engagement",
            "pages_manage_metadata"
        };

        var authUrl = $"{FB_AUTH_URL}?" +
            $"client_id={Uri.EscapeDataString(appId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(string.Join(",", scopes))}" +
            $"&state={Uri.EscapeDataString(state)}";

        return authUrl;
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, string state)
    {
        var parts = state.Split(':');
        if (parts.Length < 2) throw new ArgumentException("Invalid state parameter");

        var userId = int.Parse(parts[0]);
        var businessId = int.Parse(parts[1]);

        var appId = _configuration["Facebook:AppId"];
        var appSecret = _configuration["Facebook:AppSecret"];
        var redirectUri = _configuration["Facebook:RedirectUri"];

        var client = _httpClientFactory.CreateClient();

        var tokenUrl = $"{FB_TOKEN_URL}?" +
            $"client_id={appId}" +
            $"&client_secret={appSecret}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code={code}";

        var response = await client.GetAsync(tokenUrl);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Facebook token exchange failed: {Error}", error);
            throw new Exception($"Token exchange failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(json);

        // Exchange short-lived token for long-lived token
        var longLivedTokenUrl = $"{FB_TOKEN_URL}?" +
            $"grant_type=fb_exchange_token" +
            $"&client_id={appId}" +
            $"&client_secret={appSecret}" +
            $"&fb_exchange_token={tokenData.access_token}";

        var longLivedResponse = await client.GetAsync(longLivedTokenUrl);
        var longLivedJson = await longLivedResponse.Content.ReadAsStringAsync();
        var longLivedToken = JsonSerializer.Deserialize<FacebookTokenResponse>(longLivedJson);

        // Save to database
        var connection = await _context.PlatformConnections
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Platform == ReviewPlatform.Facebook);

        if (connection == null)
        {
            connection = new PlatformConnection
            {
                BusinessId = businessId,
                Platform = ReviewPlatform.Facebook,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            };
            _context.PlatformConnections.Add(connection);
        }

        connection.AccessToken = longLivedToken.access_token;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(longLivedToken.expires_in);
        connection.LastSyncedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Facebook OAuth token saved for business {BusinessId}", businessId);

        return "Facebook connected successfully";
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

        var reviews = new List<Review>();

        try
        {
            // First, get the pages managed by this user
            var pagesUrl = $"{FB_API_BASE}/me/accounts?access_token={connection.AccessToken}";
            var pagesResponse = await client.GetAsync(pagesUrl);

            if (!pagesResponse.IsSuccessStatusCode)
            {
                var error = await pagesResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch Facebook pages: {Error}", error);
                throw new Exception($"Failed to fetch pages: {error}");
            }

            var pagesJson = await pagesResponse.Content.ReadAsStringAsync();
            var pagesData = JsonSerializer.Deserialize<FacebookPagesResponse>(pagesJson);

            if (pagesData?.data == null || pagesData.data.Length == 0)
            {
                _logger.LogWarning("No Facebook pages found");
                return reviews;
            }

            // Get reviews for each page
            foreach (var page in pagesData.data)
            {
                var reviewsUrl = $"{FB_API_BASE}/{page.id}/ratings?" +
                    $"fields=review_text,rating,reviewer,created_time" +
                    $"&access_token={page.access_token}";

                var reviewsResponse = await client.GetAsync(reviewsUrl);

                if (!reviewsResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch reviews for page {PageId}", page.id);
                    continue;
                }

                var reviewsJson = await reviewsResponse.Content.ReadAsStringAsync();
                var reviewsData = JsonSerializer.Deserialize<FacebookReviewsResponse>(reviewsJson);

                if (reviewsData?.data == null) continue;

                foreach (var fbReview in reviewsData.data)
                {
                    // Check if review already exists
                    var reviewId = $"{page.id}_{fbReview.created_time}";
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.PlatformReviewId == reviewId);

                    if (existingReview != null) continue;

                    var review = new Review
                    {
                        BusinessId = connection.BusinessId,
                        Platform = ReviewPlatform.Facebook,
                        PlatformReviewId = reviewId,
                        ReviewerName = fbReview.reviewer?.name ?? "Facebook User",
                        Rating = fbReview.rating,
                        ReviewText = fbReview.review_text ?? "",
                        ReviewDate = DateTime.Parse(fbReview.created_time),
                        Sentiment = DetermineSentiment(fbReview.rating),
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

                _logger.LogInformation("Fetched {Count} new Facebook reviews for business {BusinessId}",
                    reviews.Count, connection.BusinessId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Facebook reviews");
            throw;
        }

        return reviews;
    }

    public async Task RefreshAccessTokenAsync(int platformConnectionId)
    {
        var connection = await _context.PlatformConnections.FindAsync(platformConnectionId);
        if (connection == null)
            throw new Exception("Platform connection not found");

        var appId = _configuration["Facebook:AppId"];
        var appSecret = _configuration["Facebook:AppSecret"];

        var client = _httpClientFactory.CreateClient();

        // Facebook doesn't use refresh tokens for long-lived tokens
        // Long-lived tokens last 60 days and need to be renewed before expiration
        var renewUrl = $"{FB_TOKEN_URL}?" +
            $"grant_type=fb_exchange_token" +
            $"&client_id={appId}" +
            $"&client_secret={appSecret}" +
            $"&fb_exchange_token={connection.AccessToken}";

        var response = await client.GetAsync(renewUrl);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Facebook token refresh failed: {Error}", error);
            throw new Exception($"Token refresh failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(json);

        connection.AccessToken = tokenData.access_token;
        connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.expires_in);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Facebook access token refreshed for connection {ConnectionId}", platformConnectionId);
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

// DTOs for Facebook API responses
public class FacebookTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string token_type { get; set; } = string.Empty;
    public int expires_in { get; set; }
}

public class FacebookPagesResponse
{
    public FacebookPage[]? data { get; set; }
}

public class FacebookPage
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string access_token { get; set; } = string.Empty;
}

public class FacebookReviewsResponse
{
    public FacebookReview[]? data { get; set; }
}

public class FacebookReview
{
    public int rating { get; set; }
    public string? review_text { get; set; }
    public string created_time { get; set; } = string.Empty;
    public FacebookReviewer? reviewer { get; set; }
}

public class FacebookReviewer
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
}
