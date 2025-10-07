using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface ICompetitorService
{
    /// <summary>
    /// Search for businesses by name and location using Google Places API
    /// </summary>
    Task<List<CompetitorSearchResult>> SearchCompetitorsAsync(string businessName, string location);

    /// <summary>
    /// Get detailed information about a competitor from Google Places
    /// </summary>
    Task<CompetitorDetails?> GetCompetitorDetailsAsync(string placeId);

    /// <summary>
    /// Get reviews for a competitor from Google Places
    /// </summary>
    Task<List<CompetitorReview>> GetCompetitorReviewsAsync(string placeId);

    /// <summary>
    /// Refresh competitor data from Google Places
    /// </summary>
    Task<Competitor> RefreshCompetitorDataAsync(int competitorId);

    /// <summary>
    /// Add a competitor to track
    /// </summary>
    Task<Competitor> AddCompetitorAsync(int businessId, string placeId, string name);

    /// <summary>
    /// Get all tracked competitors for a business
    /// </summary>
    Task<List<Competitor>> GetCompetitorsAsync(int businessId);

    /// <summary>
    /// Remove a tracked competitor
    /// </summary>
    Task<bool> RemoveCompetitorAsync(int competitorId, int businessId);
}

public class CompetitorSearchResult
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int? TotalReviews { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public bool IsOpen { get; set; }
    public string? PriceLevel { get; set; }
}

public class CompetitorDetails
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public int TotalReviews { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public List<string> Photos { get; set; } = new();
    public Dictionary<string, string> OpeningHours { get; set; } = new();
    public string? PriceLevel { get; set; }
    public List<string> Types { get; set; } = new();
}

public class CompetitorReview
{
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorPhotoUrl { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string RelativeTime { get; set; } = string.Empty;
}
