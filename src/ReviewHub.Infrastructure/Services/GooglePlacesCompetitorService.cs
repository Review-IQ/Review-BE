using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class GooglePlacesCompetitorService : ICompetitorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePlacesCompetitorService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly string _apiKey;

    public GooglePlacesCompetitorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GooglePlacesCompetitorService> logger,
        ApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _apiKey = _configuration["GooglePlaces:ApiKey"] ?? throw new ArgumentNullException("GooglePlaces:ApiKey not configured");
    }

    public async Task<List<CompetitorSearchResult>> SearchCompetitorsAsync(string businessName, string location)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var query = $"{businessName} {location}";
            var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={Uri.EscapeDataString(query)}&key={_apiKey}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GooglePlacesTextSearchResponse>(content);

            if (data?.Results == null)
            {
                return new List<CompetitorSearchResult>();
            }

            return data.Results.Select(r => new CompetitorSearchResult
            {
                PlaceId = r.PlaceId ?? string.Empty,
                Name = r.Name ?? string.Empty,
                Address = r.FormattedAddress ?? string.Empty,
                Rating = r.Rating,
                TotalReviews = r.UserRatingsTotal,
                IsOpen = r.OpeningHours?.OpenNow ?? false,
                PriceLevel = r.PriceLevel.HasValue ? new string('$', r.PriceLevel.Value) : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching competitors for {BusinessName} in {Location}", businessName, location);
            return new List<CompetitorSearchResult>();
        }
    }

    public async Task<CompetitorDetails?> GetCompetitorDetailsAsync(string placeId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var fields = "place_id,name,formatted_address,rating,user_ratings_total,formatted_phone_number,website,url,photos,opening_hours,price_level,types";
            var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields={fields}&key={_apiKey}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GooglePlacesDetailsResponse>(content);

            if (data?.Result == null)
            {
                return null;
            }

            var result = data.Result;
            var details = new CompetitorDetails
            {
                PlaceId = result.PlaceId ?? string.Empty,
                Name = result.Name ?? string.Empty,
                Address = result.FormattedAddress ?? string.Empty,
                Rating = result.Rating,
                TotalReviews = result.UserRatingsTotal ?? 0,
                PhoneNumber = result.FormattedPhoneNumber,
                Website = result.Website,
                GoogleMapsUrl = result.Url,
                PriceLevel = result.PriceLevel.HasValue ? new string('$', result.PriceLevel.Value) : null,
                Types = result.Types ?? new List<string>()
            };

            // Get photo URLs (limit to 5)
            if (result.Photos != null)
            {
                details.Photos = result.Photos.Take(5).Select(p =>
                    $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=400&photoreference={p.PhotoReference}&key={_apiKey}"
                ).ToList();
            }

            // Parse opening hours
            if (result.OpeningHours?.WeekdayText != null)
            {
                foreach (var day in result.OpeningHours.WeekdayText)
                {
                    var parts = day.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        details.OpeningHours[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor details for place_id {PlaceId}", placeId);
            return null;
        }
    }

    public async Task<List<CompetitorReview>> GetCompetitorReviewsAsync(string placeId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=reviews&key={_apiKey}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GooglePlacesDetailsResponse>(content);

            if (data?.Result?.Reviews == null)
            {
                return new List<CompetitorReview>();
            }

            return data.Result.Reviews.Select(r => new CompetitorReview
            {
                AuthorName = r.AuthorName ?? "Anonymous",
                AuthorPhotoUrl = r.ProfilePhotoUrl ?? string.Empty,
                Rating = r.Rating,
                Text = r.Text ?? string.Empty,
                Time = DateTimeOffset.FromUnixTimeSeconds(r.Time).DateTime,
                RelativeTime = r.RelativeTimeDescription ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting competitor reviews for place_id {PlaceId}", placeId);
            return new List<CompetitorReview>();
        }
    }

    public async Task<Competitor> RefreshCompetitorDataAsync(int competitorId)
    {
        var competitor = await _context.Competitors.FindAsync(competitorId);
        if (competitor == null)
        {
            throw new InvalidOperationException("Competitor not found");
        }

        var details = await GetCompetitorDetailsAsync(competitor.PlatformBusinessId);
        if (details != null)
        {
            competitor.Name = details.Name;
            competitor.TotalReviews = details.TotalReviews;
            competitor.CurrentRating = details.Rating ?? 0;
            competitor.LastCheckedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return competitor;
    }

    public async Task<Competitor> AddCompetitorAsync(int businessId, string placeId, string name)
    {
        // Check if already tracking this competitor
        var existing = await _context.Competitors
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformBusinessId == placeId);

        if (existing != null)
        {
            throw new InvalidOperationException("This competitor is already being tracked");
        }

        // Get details from Google Places
        var details = await GetCompetitorDetailsAsync(placeId);

        var competitor = new Competitor
        {
            BusinessId = businessId,
            PlatformBusinessId = placeId,
            Name = details?.Name ?? name,
            Platform = Core.Enums.ReviewPlatform.Google,
            TotalReviews = details?.TotalReviews ?? 0,
            CurrentRating = details?.Rating ?? 0,
            LastCheckedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Competitors.Add(competitor);
        await _context.SaveChangesAsync();

        return competitor;
    }

    public async Task<List<Competitor>> GetCompetitorsAsync(int businessId)
    {
        return await _context.Competitors
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.LastCheckedAt)
            .ToListAsync();
    }

    public async Task<bool> RemoveCompetitorAsync(int competitorId, int businessId)
    {
        var competitor = await _context.Competitors
            .FirstOrDefaultAsync(c => c.Id == competitorId && c.BusinessId == businessId);

        if (competitor == null)
        {
            return false;
        }

        _context.Competitors.Remove(competitor);
        await _context.SaveChangesAsync();

        return true;
    }
}

// Google Places API Response Models
public class GooglePlacesTextSearchResponse
{
    public List<GooglePlaceResult>? Results { get; set; }
    public string? Status { get; set; }
}

public class GooglePlacesDetailsResponse
{
    public GooglePlaceResult? Result { get; set; }
    public string? Status { get; set; }
}

public class GooglePlaceResult
{
    public string? PlaceId { get; set; }
    public string? Name { get; set; }
    public string? FormattedAddress { get; set; }
    public double? Rating { get; set; }
    public int? UserRatingsTotal { get; set; }
    public string? FormattedPhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? Url { get; set; }
    public int? PriceLevel { get; set; }
    public List<string>? Types { get; set; }
    public GooglePlaceOpeningHours? OpeningHours { get; set; }
    public List<GooglePlacePhoto>? Photos { get; set; }
    public List<GooglePlaceReview>? Reviews { get; set; }
}

public class GooglePlaceOpeningHours
{
    public bool OpenNow { get; set; }
    public List<string>? WeekdayText { get; set; }
}

public class GooglePlacePhoto
{
    public string? PhotoReference { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class GooglePlaceReview
{
    public string? AuthorName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public int Rating { get; set; }
    public string? Text { get; set; }
    public long Time { get; set; }
    public string? RelativeTimeDescription { get; set; }
}
