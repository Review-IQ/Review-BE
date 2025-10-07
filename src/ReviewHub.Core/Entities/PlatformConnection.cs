using ReviewHub.Core.Enums;

namespace ReviewHub.Core.Entities;

public class PlatformConnection
{
    public int Id { get; set; }
    public int BusinessId { get; set; } // Legacy - kept for backward compatibility
    public int? LocationId { get; set; } // NEW: Which specific location this platform is connected to
    public ReviewPlatform Platform { get; set; }

    // OAuth tokens (encrypted at rest)
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime TokenExpiresAt { get; set; }

    // Platform-specific identifiers
    public string? PlatformBusinessId { get; set; }
    public string? PlatformBusinessName { get; set; }
    public string? PlatformAccountId { get; set; }
    public string? PlatformAccountEmail { get; set; }

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Sync settings
    public bool AutoSync { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 60; // Default: sync every hour

    // Navigation
    public Business Business { get; set; } = null!;
    public Entities.Location? PlatformLocation { get; set; } // NEW: Reference to Location entity
}
