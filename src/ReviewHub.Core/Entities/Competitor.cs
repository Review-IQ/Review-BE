using ReviewHub.Core.Enums;

namespace ReviewHub.Core.Entities;

public class Competitor
{
    public int Id { get; set; }
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ReviewPlatform Platform { get; set; }
    public string PlatformBusinessId { get; set; } = string.Empty;

    // Tracking
    public double? CurrentRating { get; set; }
    public int? TotalReviews { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Business Business { get; set; } = null!;
}
