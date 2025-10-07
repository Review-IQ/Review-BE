namespace ReviewHub.Core.Entities;

/// <summary>
/// Controls which locations a user can access
/// Supports: All Locations, Specific Locations, or Location Groups
/// </summary>
public class UserLocationAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrganizationId { get; set; }

    // Access Patterns (mutually exclusive)
    /// <summary>
    /// If true, user has access to ALL locations in the organization
    /// Use for: CEO, Admins
    /// </summary>
    public bool HasAllLocationsAccess { get; set; } = false;

    /// <summary>
    /// Specific location ID (if accessing a single location)
    /// Use for: Store managers
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Location group ID (if accessing a group of locations)
    /// Use for: Regional managers
    /// </summary>
    public int? LocationGroupId { get; set; }

    // Permissions for this access (JSON: {canEdit: true, canRespond: true, ...})
    public string? Permissions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Location? Location { get; set; }
    public LocationGroup? LocationGroup { get; set; }
}
